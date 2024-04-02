using System.Linq;
using System.Numerics;
using Content.Server._White.Other;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Robust.Server.Audio;
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
    [Dependency] private readonly IRobustRandom _random = default!;

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

        if (TryComp<SharedPullableComponent>(args.User, out var pullable) && pullable.BeingPulled)
        {
            _pullingSystem.TryStopPull(pullable);
        }

        if (TryComp<SharedPullerComponent>(args.User, out var pulling)
            && pulling.Pulling != null &&
            TryComp<SharedPullableComponent>(pulling.Pulling.Value, out var subjectPulling))
        {
            _pullingSystem.TryStopPull(subjectPulling);
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

        SoundAndEffects(component, coords, oldCoords);

        _transform.SetCoordinates(args.User, coords);

        component.Uses--;
        component.NextUse = _timing.CurTime + component.Cooldown;
    }

    private void OnExamine(EntityUid uid, ExperimentalSyndicateTeleporterComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("experimental-syndicate-teleporter-examine", ("uses", component.Uses)));
    }

    private void EmergencyTeleportation(EntityUid uid, TransformComponent xform, ExperimentalSyndicateTeleporterComponent component, EntityCoordinates oldCoords, Vector2 offset)
    {
        var newOffset = offset + VectorRandomDirection(component, offset, component.EmergencyLength);

        var coords = xform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

        SoundAndEffects(component, coords, oldCoords);

        _transform.SetCoordinates(uid, coords);

        component.Uses--;
        component.NextUse = _timing.CurTime + component.Cooldown;

        if (TryCheckWall(coords))
        {
            _bodySystem.GibBody(uid, true, splatModifier: 3F);
        }
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
