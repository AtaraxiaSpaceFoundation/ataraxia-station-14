using Content.Server.Hands.Systems;
using Content.Server.Stunnable;
using Content.Server._White.Cult.Items.Components;
using Content.Shared._White.Chaplain;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.Items.Systems;

public sealed class ReturnItemOnThrowSystem : EntitySystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly HolyWeaponSystem _holyWeapon = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReturnItemOnThrowComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnThrowHit(EntityUid uid, ReturnItemOnThrowComponent component, ThrowDoHitEvent args)
    {
        var isCultist = HasComp<CultistComponent>(args.Target);
        var thrower = args.Component.Thrower;
        if (!HasComp<CultistComponent>(thrower))
            return;

        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (!_stun.IsParalyzed(args.Target) && !isCultist && !_holyWeapon.IsHoldingHolyWeapon(args.Target))
        {
            _stun.TryParalyze(args.Target, TimeSpan.FromSeconds(component.StunTime), true);
        }

        _hands.PickupOrDrop(thrower, uid);
    }
}
