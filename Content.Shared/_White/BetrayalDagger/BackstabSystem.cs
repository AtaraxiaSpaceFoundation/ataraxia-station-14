using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.BetrayalDagger;

public sealed class BackstabSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BackstabComponent, MeleeHitEvent>(HandleHit);
    }

    private void HandleHit(Entity<BackstabComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count != 1)
            return;

        var target = args.HitEntities[0];

        if (target == args.User || !HasComp<MobStateComponent>(target) ||
            !TryComp(target, out TransformComponent? xform))
            return;

        var rot1 = _transform.GetWorldRotation(args.User).FlipPositive();
        var rot2 = _transform.GetWorldRotation(xform).FlipPositive();
        var tol = ent.Comp.Tolerance;

        if (!MathHelper.CloseTo(rot1, rot2, tol) &&
            !MathHelper.CloseTo(rot1, rot2 + MathHelper.TwoPi, tol) &&
            !MathHelper.CloseTo(rot1 + MathHelper.TwoPi, rot2, tol))
            return;

        var damage = args.BaseDamage.GetTotal() * ent.Comp.DamageMultiplier;

        args.BonusDamage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"),
            damage - args.BaseDamage.GetTotal());

        args.PenetrateArmor = ent.Comp.PenetrateArmor;

        if (_net.IsServer)
            _popup.PopupEntity($@"Backstab! {damage}", args.User, PopupType.MediumCaution);
    }
}
