using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.Other.CritSystem;

public sealed class CritSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CritComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CritComponent, MeleeHitEvent>(HandleHit);
        SubscribeLocalEvent<CritComponent, GetMeleeAttackRateEvent>(GetMeleeAttackRate);
        SubscribeLocalEvent<BloodLustComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    }

    private void OnRefreshMoveSpeed(Entity<BloodLustComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = GetBloodLustModifier(ent);
        args.ModifySpeed(GetBloodLustMultiplier(ent.Comp.WalkModifier, modifier),
            GetBloodLustMultiplier(ent.Comp.SprintModifier, modifier));
    }

    private void GetMeleeAttackRate(Entity<CritComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (!ent.Comp.IsBloodDagger)
            return;

        if (!TryComp(args.User, out BloodLustComponent? bloodLust))
            return;

        args.Multipliers *= GetBloodLustMultiplier(bloodLust.AttackRateModifier, GetBloodLustModifier(args.User));
    }

    private float GetBloodLustModifier(EntityUid uid)
    {
        if (!TryComp(uid, out BloodstreamComponent? bloodstream) || bloodstream.MaxBleedAmount == 0f)
            return 1f;

        return Math.Clamp(bloodstream.BleedAmount / bloodstream.MaxBleedAmount, 0f, 1f);
    }

    private float GetBloodLustMultiplier(float multiplier, float modifier)
    {
        return float.Lerp(1f, multiplier, modifier);
    }

    private void OnExamine(EntityUid uid, CritComponent component, ExaminedEvent args)
    {
        if (component.IsBloodDagger)
        {
            args.PushMarkup(
                "[color=red]Критическая жажда: Кинжал Жажды обладает смертоносной точностью. Его владелец имеет 50% шанс нанести критический урон, поражая врага в его самые уязвимые места.\n" +
                "При ударе по себе кинжал наделит пользователя временным усилением скорости атаки и передвижения ценой обильного кровотечения.\n" +
                "Кровавый абсорб: При каждом успешном критическом ударе, кинжал извлекает кровь из цели, восстанавливая здоровье владельцу пропорционально количеству высосанной крови.[/color]"
            );
        }
    }

    private void HandleHit(EntityUid uid, CritComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (args.HitEntities[0] == args.User)
        {
            if (!component.IsBloodDagger)
                return;

            if (!TryComp(args.User, out BloodstreamComponent? bloodstream))
                return;

            EnsureComp<BloodLustComponent>(args.User);
            _bloodstream.TryModifyBleedAmount(args.User, bloodstream.MaxBleedAmount, bloodstream);
            return;
        }

        if (!IsCriticalHit(component))
            return;

        var ohio = 0;

        if (component.IsBloodDagger)
        {
            var bruteGroup = _prototypeManager.Index<DamageGroupPrototype>("Brute");
            var burnGroup = _prototypeManager.Index<DamageGroupPrototype>("Burn");

            ohio = _random.Next(1, 21);

            foreach (var target in args.HitEntities)
            {
                if (!TryComp(target, out BloodstreamComponent? bloodstream))
                    continue;

                if (!_bloodstream.TryModifyBloodLevel(target, -ohio, bloodstream, false))
                    continue;

                _bloodstream.TryModifyBloodLevel(args.User, ohio);
                _damageableSystem.TryChangeDamage(args.User, new DamageSpecifier(bruteGroup, -ohio));
                _damageableSystem.TryChangeDamage(args.User, new DamageSpecifier(burnGroup, -ohio));
            }
        }

        var damage = args.BaseDamage.GetTotal() * component.CritMultiplier + ohio;

        args.BonusDamage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"),
            damage - args.BaseDamage.GetTotal());

        _popup.PopupEntity($"Crit! {damage}", args.User, args.User, PopupType.MediumCaution);
    }

    private bool IsCriticalHit(CritComponent component)
    {
        var roll = _random.Next(1, 101);

        var critChance = component.CritChance;

        component.WorkingChance ??= component.CritChance;

        var isCritical = roll <= component.WorkingChance;

        if (isCritical)
            component.WorkingChance = critChance;
        else
            component.WorkingChance++;

        return isCritical;
    }
}

