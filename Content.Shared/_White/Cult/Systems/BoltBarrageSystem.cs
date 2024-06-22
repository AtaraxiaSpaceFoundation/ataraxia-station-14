using System.Linq;
using Content.Shared._White.Cult.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._White.Cult.Systems;

public sealed class BoltBarrageSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BoltBarrageComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<BoltBarrageComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<BoltBarrageComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<BoltBarrageComponent, GotUnequippedHandEvent>(OnUnequipHand);
        SubscribeLocalEvent<BoltBarrageComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<BoltBarrageComponent, OnEmptyGunShotEvent>(OnEmptyShot);
        SubscribeLocalEvent<BoltBarrageComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<BoltBarrageComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("bolt-barrage-component-extra-desc"));
    }

    private void OnUnequipHand(Entity<BoltBarrageComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_net.IsServer && ent.Comp.Unremoveable)
            QueueDel(ent);
    }

    private void OnRemoveAttempt(Entity<BoltBarrageComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (!_timing.ApplyingState && ent.Comp.Unremoveable)
            args.Cancel();
    }

    private void OnEmptyShot(Entity<BoltBarrageComponent> ent, ref OnEmptyGunShotEvent args)
    {
        if (_net.IsServer)
            QueueDel(ent);
    }

    private void OnDrop(Entity<BoltBarrageComponent> ent, ref DroppedEvent args)
    {
        if (_net.IsServer && ent.Comp.Unremoveable)
            QueueDel(ent);
    }

    private void OnGunShot(Entity<BoltBarrageComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp(args.User, out HandsComponent? hands))
            return;

        foreach (var hand in _hands.EnumerateHands(args.User, hands))
        {
            if (!hand.IsEmpty)
                continue;

            ent.Comp.Unremoveable = false;
            _hands.SetActiveHand(args.User, hand, hands);
            _hands.TryPickup(args.User, ent, hand, false, false, hands);
            ent.Comp.Unremoveable = true;
            return;
        }
    }

    private void OnShootAttempt(Entity<BoltBarrageComponent> ent, ref AttemptShootEvent args)
    {
        /*if (!HasComp<CultistComponent>(args.User) && !HasComp<GhostComponent>(args.User))
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("bolt-barrage-component-not-cultist");
            return;
        }*/

        if (_hands.EnumerateHands(args.User).Any(hand => hand.IsEmpty))
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString("bolt-barrage-component-no-empty-hand");
    }
}
