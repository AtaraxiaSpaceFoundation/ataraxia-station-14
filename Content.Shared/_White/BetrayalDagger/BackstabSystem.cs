using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
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

        var userXform = Transform(args.User);
        var v1 = (_transform.GetWorldRotation(xform) + MathHelper.PiOver2).ToVec(); // Flipped WorldVec
        var v2 = _transform.GetWorldPosition(userXform) - _transform.GetWorldPosition(xform);
        var angle = Vector3.CalculateAngle(new Vector3(v1), new Vector3(v2));

        if (angle > ent.Comp.Tolerance.Theta)
            return;

        var damage = args.BaseDamage.GetTotal() * ent.Comp.DamageMultiplier;

        args.BonusDamage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"),
            damage - args.BaseDamage.GetTotal());

        args.PenetrateArmor = ent.Comp.PenetrateArmor;

    }
}
