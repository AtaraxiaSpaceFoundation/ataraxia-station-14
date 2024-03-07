using System.Linq;
using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Magic;
using Content.Shared.Magic.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;

namespace Content.Server.Magic;

/// <summary>
/// Handles learning and using spells (actions)
/// </summary>
public sealed class MagicSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DoorBoltSystem _boltsSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstantSpawnSpellEvent>(OnInstantSpawn);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
        SubscribeLocalEvent<SmiteSpellEvent>(OnSmiteSpell);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnWorldSpawn);
        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
        SubscribeLocalEvent<ChangeComponentsSpellEvent>(OnChangeComponentsSpell);
    }

    #region Spells

    private void OnInstantSpawn(InstantSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        foreach (var position in GetSpawnPositions(transform, args.Pos))
        {
            var ent = Spawn(args.Prototype, position.SnapToGrid(EntityManager, _mapManager));

            if (!args.PreventCollideWithCaster)
                continue;

            var comp = EnsureComp<PreventCollideComponent>(ent);
            comp.Uid = args.Performer;
        }

        Speak(args);
        args.Handled = true;
    }

    private void OnProjectileSpell(ProjectileSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var xform = Transform(ev.Performer);

        foreach (var pos in GetSpawnPositions(xform, ev.Pos))
        {
            var mapPos = _transformSystem.ToMapCoordinates(pos);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out var grid)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var userVelocity = Vector2.Zero;

            if (grid != null && TryComp(gridUid, out PhysicsComponent? physics))
                userVelocity = physics.LinearVelocity;

            var ent = Spawn(ev.Prototype, spawnCoords);
            var direction = ev.Target.ToMapPos(EntityManager, _transformSystem) -
                            spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, ev.Performer, ev.Performer);
        }
    }

    private void OnChangeComponentsSpell(ChangeComponentsSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;

        Speak(ev);

        foreach (var toRemove in ev.ToRemove)
        {
            if (_compFact.TryGetRegistration(toRemove, out var registration))
                RemComp(ev.Target, registration.Type);
        }

        foreach (var (name, data) in ev.ToAdd)
        {
            if (HasComp(ev.Target, data.Component.GetType()))
                continue;

            var component = (Component) _compFact.GetComponent(name);
            var temp = (object) component;
            _serializationManager.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(ev.Target, (Component) temp!);
        }
    }

    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        _transformSystem.SetCoordinates(args.Performer, args.Target);
        _transformSystem.AttachToGridOrMap(args.Performer);
        _audio.PlayPvs(args.BlinkSound, args.Performer, AudioParams.Default.WithVolume(args.BlinkVolume));
        Speak(args);
        args.Handled = true;
    }

    private void OnKnockSpell(KnockSpellEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Speak(args);

        var transform = Transform(args.Performer);
        var coords = transform.Coordinates;

        _audio.PlayPvs(args.KnockSound, args.Performer, AudioParams.Default.WithVolume(args.KnockVolume));

        foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
        {
            if (TryComp<DoorBoltComponent>(entity, out var bolts))
                _boltsSystem.SetBoltsDown(entity, bolts, false);

            if (TryComp<DoorComponent>(entity, out var doorComp) && doorComp.State is not DoorState.Open)
                _doorSystem.StartOpening(entity);
        }
    }

    private void OnSmiteSpell(SmiteSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;

        Speak(ev);

        var direction = _transformSystem.GetMapCoordinates(ev.Target).Position - _transformSystem.GetMapCoordinates(ev.Performer).Position;
        var impulseVector = direction * 10000;

        _physics.ApplyLinearImpulse(ev.Target, impulseVector);

        if (!TryComp<BodyComponent>(ev.Target, out var body))
            return;

        var entities = _bodySystem.GibBody(ev.Target, true, body);

        if (!ev.DeleteNonBrainParts)
            return;

        foreach (var part in entities.Where(part => HasComp<BodyComponent>(part) && !HasComp<BrainComponent>(part)))
        {
            QueueDel(part);
        }
    }

    private void OnWorldSpawn(WorldSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var targetMapCoords = args.Target;

        SpawnSpellHelper(args.Contents, targetMapCoords, args.Lifetime, args.Offset);
        Speak(args);
        args.Handled = true;
    }

    #endregion

    #region Helpers

    public List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, MagicSpawnData data)
    {
        return data switch
        {
            TargetCasterPos => GetCasterPosition(casterXform),
            TargetInFront => GetPositionsInFront(casterXform),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public List<EntityCoordinates> GetCasterPosition(TransformComponent casterXform)
    {
        return new List<EntityCoordinates>(1) { casterXform.Coordinates };
    }

    public List<EntityCoordinates> GetPositionsInFront(TransformComponent casterXform)
    {
        var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

        if (!TryComp<MapGridComponent>(casterXform.GridUid, out var mapGrid) ||
            !directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
        {
            return new List<EntityCoordinates>();
        }

        var tileIndex = tileReference.Value.GridIndices;
        var coords = _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex);

        var directions = GetCardinalDirections(casterXform.LocalRotation.GetCardinalDir());
        var spawnPositions = new List<EntityCoordinates>(3);

        foreach (var direction in directions)
        {
            var offset = GetOffsetForDirection(direction);
            var coordinates = _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex + offset);
            spawnPositions.Add(coordinates);
        }

        spawnPositions.Add(coords);
        return spawnPositions;
    }

    public IEnumerable<Direction> GetCardinalDirections(Direction dir)
    {
        switch (dir)
        {
            case Direction.North:
            case Direction.South:
                return new[] { Direction.North, Direction.South };
            case Direction.East:
            case Direction.West:
                return new[] { Direction.East, Direction.West };
            default:
                return Array.Empty<Direction>();
        }
    }

    public (int, int) GetOffsetForDirection(Direction direction)
    {
        return direction switch
        {
            Direction.North => (1, 0),
            Direction.South => (-1, 0),
            Direction.East => (0, 1),
            Direction.West => (0, -1),
            _ => (0, 0)
        };
    }

    public void SpawnSpellHelper(List<EntitySpawnEntry> entityEntries, EntityCoordinates entityCoords, float? lifetime, Vector2 offsetVector2)
    {
        var getPrototypes = EntitySpawnCollection.GetSpawns(entityEntries, _random);

        var offsetCoords = entityCoords;
        foreach (var proto in getPrototypes)
        {
            // TODO: Share this code with instant because they're both doing similar things for positioning.
            var entity = Spawn(proto, offsetCoords);
            offsetCoords = offsetCoords.Offset(offsetVector2);

            if (lifetime != null)
            {
                var comp = EnsureComp<TimedDespawnComponent>(entity);
                comp.Lifetime = lifetime.Value;
            }
        }
    }

    private void Speak(BaseActionEvent args)
    {
        if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);
    }

    #endregion
}
