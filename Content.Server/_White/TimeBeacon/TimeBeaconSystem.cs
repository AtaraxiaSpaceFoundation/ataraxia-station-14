using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._White.TimeBeacon;

public sealed class TimeBeaconSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimeBeaconComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TimeBeaconComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<TimeBeaconAnchorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TimeBeaconAnchorComponent> ent, ref MapInitEvent args)
    {
        Timer.Spawn(ent.Comp.Duration, () =>
        {
            var entity = ent.Comp.Entity;

            if (entity == EntityUid.Invalid)
                return;

            if (EntityManager.Deleted(ent) || EntityManager.Deleted(entity))
                return;

            if (!TryComp(ent, out TransformComponent? xform) || !TryComp(entity, out TransformComponent? entXform))
                return;

            // If entity polymorphed or something
            if (_mapManager.IsMapPaused(entXform.MapID))
                return;

            // break pulls before portal enter so we dont break shit
            if (TryComp<PullableComponent>(entity, out var pullable) && pullable.BeingPulled)
            {
                _pulling.TryStopPull(entity, pullable);
            }

            if (TryComp<PullerComponent>(entity, out var pulling)
                && pulling.Pulling != null &&
                TryComp<PullableComponent>(pulling.Pulling.Value, out var subjectPulling))
            {
                _pulling.TryStopPull(pulling.Pulling.Value, subjectPulling);
            }

            _transform.SetCoordinates(entity, entXform, xform.Coordinates);
            QueueDel(ent);
        });
    }

    private void OnExamine(Entity<TimeBeaconComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.NextUse <= _timing.CurTime)
        {
            args.PushMarkup(Loc.GetString("time-beacon-component-charged"));
            return;
        }

        var message = Loc.GetString("time-beacon-component-charging",
            ("cooldown", (int) (ent.Comp.NextUse - _timing.CurTime).TotalSeconds));
        args.PushMarkup(message);
    }

    private void OnUseInHand(Entity<TimeBeaconComponent> ent, ref UseInHandEvent args)
    {
        var coords = CompOrNull<TransformComponent>(args.User)?.Coordinates;

        if (coords == null)
            return;

        if (ent.Comp.NextUse > _timing.CurTime)
        {
            var message = Loc.GetString("time-beacon-component-cooldown",
                ("cooldown", (int) (ent.Comp.NextUse - _timing.CurTime).TotalSeconds));
            _popup.PopupEntity(message, args.User, args.User);
            return;
        }

        var anchor = Spawn(ent.Comp.AnchorEntity, coords.Value);
        _transform.AttachToGridOrMap(anchor);
        var anchorComp = EnsureComp<TimeBeaconAnchorComponent>(anchor);
        anchorComp.Entity = args.User;

        _popup.PopupEntity(Loc.GetString("time-beacon-component-anchor-set"), args.User, args.User, PopupType.Medium);
        _audio.PlayEntity(ent.Comp.Sound, args.User, ent);

        ent.Comp.NextUse = _timing.CurTime + ent.Comp.Cooldown;
    }
}
