using System.Linq;
using System.Numerics;
using Content.Server._White.Other;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Shared._White.BetrayalDagger;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.ExperimentalSyndicateTeleporter;
public sealed class ExperimentalSyndicateTeleporter : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TelefragSystem _telefrag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExperimentalSyndicateTeleporterComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<ExperimentalSyndicateTeleporterComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExperimentalSyndicateTeleporterComponent>();
        while (query.MoveNext(out var component))
        {
            if (component.Uses >= 4)
                continue;

            component.ChargeCooldown += frameTime;

            if (component.ChargeCooldown <= component.NextRechargeAttempt.TotalSeconds)
                continue;

            component.ChargeCooldown = 0F;

            if (!_random.Prob(0.1F))
                continue;

            component.Uses++;
        }
    }

    private void OnUse(EntityUid uid, ExperimentalSyndicateTeleporterComponent component, UseInHandEvent args)
    {
        if (component.Uses <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("experimental-syndicate-teleporter-end-uses"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (component.NextUse > _timing.CurTime)
        {
            _popupSystem.PopupEntity(Loc.GetString("experimental-syndicate-teleporter-cooldown"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!TryComp<TransformComponent>(args.User, out var xform))
            return;

        if (TryComp<PullableComponent>(args.User, out var pullable) && pullable.BeingPulled)
        {
            _pullingSystem.TryStopPull(args.User, pullable);
        }

        if (TryComp<PullerComponent>(args.User, out var pulling)
            && pulling.Pulling != null &&
            TryComp<PullableComponent>(pulling.Pulling.Value, out var subjectPulling))
        {
            _pullingSystem.TryStopPull(pulling.Pulling.Value, subjectPulling);
        }

        if (_containerSystem.IsEntityInContainer(args.User))
        {
            if(!_containerSystem.TryRemoveFromContainer(args.User))
                return;
        }

        var oldCoords = xform.Coordinates;

        var random = _random.Next(component.MinTeleportRange, component.MaxTeleportRange);
        var offset = xform.LocalRotation.ToWorldVec().Normalized();
        var direction = xform.LocalRotation.GetDir().ToVec();
        var newOffset = offset + direction * random;

        var coords = xform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

        if (TryCheckWall(coords))
        {
            EmergencyTeleportation(args.User, xform, component, oldCoords, newOffset);
            return;
        }

        Teleport(args.User, component, coords, oldCoords);
    }

    private void OnExamine(EntityUid uid, ExperimentalSyndicateTeleporterComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("experimental-syndicate-teleporter-examine", ("uses", component.Uses)));
    }

    private void EmergencyTeleportation(EntityUid uid, TransformComponent xform, ExperimentalSyndicateTeleporterComponent component, EntityCoordinates oldCoords, Vector2 offset)
    {
        var newOffset = offset + VectorRandomDirection(component, offset, component.EmergencyLength);

        var coords = xform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

        Teleport(uid, component, coords, oldCoords);

        if (TryCheckWall(coords))
        {
            _bodySystem.GibBody(uid, true, splatModifier: 3F);
        }
    }

    private void Teleport(EntityUid uid, ExperimentalSyndicateTeleporterComponent component, EntityCoordinates coords,
        EntityCoordinates oldCoords)
    {
        SoundAndEffects(component, coords, oldCoords);

        _telefrag.Telefrag(coords, uid);
        _transform.SetCoordinates(uid, coords);

        component.Uses--;
        component.NextUse = _timing.CurTime + component.Cooldown;
    }

    private void SoundAndEffects(ExperimentalSyndicateTeleporterComponent component, EntityCoordinates coords, EntityCoordinates oldCoords)
    {
        _audio.PlayPvs(component.TeleportSound, coords);
        _audio.PlayPvs(component.TeleportSound, oldCoords);

        _entManager.SpawnEntity(component.ExpSyndicateTeleportInEffect, coords);
        _entManager.SpawnEntity(component.ExpSyndicateTeleportOutEffect, oldCoords);
    }

    private bool TryCheckWall(EntityCoordinates coords)
    {
        if (!coords.TryGetTileRef(out var tile))
            return false;

        if (!TryComp<MapGridComponent>(tile.Value.GridUid, out var mapGridComponent))
            return false;

        var anchoredEntities = _mapSystem.GetAnchoredEntities(tile.Value.GridUid, mapGridComponent, coords);

        return anchoredEntities.Any(HasComp<WallMarkComponent>);
    }

    private Vector2 VectorRandomDirection(ExperimentalSyndicateTeleporterComponent component, Vector2 offset, int length)
    {
        var randomRotation = _random.Next(0, component.RandomRotations.Count);
        return Angle.FromDegrees(component.RandomRotations[randomRotation]).RotateVec(offset.Normalized() * length);
    }
}
