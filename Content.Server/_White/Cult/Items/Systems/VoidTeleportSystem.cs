using System.Threading;
using Content.Shared._White.Cult.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared._White.Cult.Items;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._White.Cult.Items.Systems;

public sealed class VoidTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoidTeleportComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<VoidTeleportComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, VoidTeleportComponent component, ComponentInit args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnUseInHand(EntityUid uid, VoidTeleportComponent component, UseInHandEvent args)
    {
        if (!HasComp<CultistComponent>(args.User))
        {
            _hands.TryDrop(args.User);
            _popup.PopupEntity(Loc.GetString("void-teleport-not-cultist"), args.User, args.User);
            return;
        }

        if (!component.Active || component.UsesLeft <= 0)
        {
            _popup.PopupEntity(Loc.GetString("void-teleport-drained"), args.User, args.User);
            return;
        }

        if (component.NextUse > _timing.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("void-teleport-cooldown"), args.User, args.User);
            return;
        }

        if (!TryComp<TransformComponent>(args.User, out var transform))
            return;

        var oldCoords = transform.Coordinates;

        EntityCoordinates coords = default;
        var foundTeleportPos = false;
        var attempts = 10;
        //Repeat until proper place for tp is found
        while (attempts > 0)
        {
            attempts--;
            //Get coords to where tp
            var random = _random.Next(component.MinRange, component.MaxRange);
            var offset = transform.LocalRotation.ToWorldVec().Normalized();
            var direction = transform.LocalRotation.GetDir().ToVec();
            var newOffset = offset + direction * random;
            coords = transform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

            var tile = coords.GetTileRef();

            //Check for walls
            if (tile != null && _turf.IsTileBlocked(tile.Value, CollisionGroup.AllMask))
                continue;

            foundTeleportPos = true;
            break;
        }

        if (!foundTeleportPos)
            return;

        CreatePulse(uid, component);

        _xform.SetCoordinates(args.User, coords);
        _transform.AttachToGridOrMap(args.User, transform);

        if (_pulling.TryGetPulledEntity(args.User, out var pulled))
        {
            _xform.SetCoordinates(pulled.Value, coords);
            _transform.AttachToGridOrMap(pulled.Value);
        }

        //Play tp sound
        _audio.PlayPvs(component.TeleportInSound, coords);
        _audio.PlayPvs(component.TeleportOutSound, oldCoords);

        //Create tp effect
        _entMan.SpawnEntity(component.TeleportInEffect, coords);
        _entMan.SpawnEntity(component.TeleportOutEffect, oldCoords);

        component.UsesLeft--;
        component.NextUse = _timing.CurTime + component.Cooldown;
    }

    private void UpdateAppearance(EntityUid uid, VoidTeleportComponent comp)
    {
        AppearanceComponent? appearance = null;
        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, VeilVisuals.Activated, comp.Active, appearance);
    }

    private void CreatePulse(EntityUid uid, VoidTeleportComponent component)
    {
        if (_pointLight.TryGetLight(uid, out var light))
        {
            _pointLight.SetEnergy(uid, 5f, light);
        }

        Timer.Spawn(component.TimerDelay, () => TurnOffPulse(uid, component), component.Token.Token);
    }

    private void TurnOffPulse(EntityUid uid, VoidTeleportComponent comp)
    {
        if (!_pointLight.TryGetLight(uid, out var light))
            return;

        _pointLight.SetEnergy(uid, 1f, light);

        comp.Token = new CancellationTokenSource();

        if (comp.UsesLeft <= 0)
        {
            comp.Active = false;
            _pointLight.SetEnabled(uid, false);

            UpdateAppearance(uid, comp);
        }
    }
}