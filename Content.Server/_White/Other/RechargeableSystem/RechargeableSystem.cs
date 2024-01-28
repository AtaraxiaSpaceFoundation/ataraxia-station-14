using Content.Server.Item;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._White.Other.RechargeableSystem;


public sealed class RechargeableSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RechargeableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RechargeableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RechargeableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RechargeableComponent, ItemToggleActivateAttemptEvent>(OnTryActivate);
    }

    private void OnTryActivate(Entity<RechargeableComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!ent.Comp.Discharged)
            return;

        args.Cancelled = true;

        _audio.PlayPvs(_audio.GetSound(ent.Comp.TurnOnFailSound), ent, AudioParams.Default.WithVariation(0.25f));
        _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), args.User ?? ent);
    }

    private void OnExamined(EntityUid uid, RechargeableComponent component, ExaminedEvent args)
    {
        if (component.Discharged)
        {
            var remainingTime = (int) (component.RechargeDelay - component.AccumulatedFrametime);
            args.PushMarkup("Он [color=red]разряжен[/color].");
            args.PushMarkup($"Осталось времени для зарядки: [color=green]{remainingTime}[/color] секунд.");
            return;
        }

        var currentCharge = (int) (100 * component.Charge / component.MaxCharge);
        args.PushMarkup($"Текущий заряд: [color=green]{currentCharge}%[/color]");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var rechargeable in EntityManager.EntityQuery<RechargeableComponent>())
        {
            if (!rechargeable.Discharged && rechargeable.Charge == rechargeable.MaxCharge)
                continue;

            rechargeable.AccumulatedFrametime += frameTime;

            var delay = rechargeable.Discharged ? rechargeable.RechargeDelay : 1f;

            if (rechargeable.AccumulatedFrametime < delay)
                continue;

            rechargeable.AccumulatedFrametime -= delay;

            if (rechargeable.Discharged)
            {
                rechargeable.Discharged = false;
                rechargeable.Charge = rechargeable.MaxCharge;
            }
            else
            {
                rechargeable.Charge = FixedPoint2.Min(rechargeable.MaxCharge,
                    rechargeable.Charge + rechargeable.ChargePerSecond);
            }
        }
    }

    private void OnDamageChanged(EntityUid uid, RechargeableComponent component, DamageChangedEvent args)
    {
        if (component.Discharged || args.DamageDelta == null)
            return;

        var totalDamage = args.DamageDelta.GetTotal();

        component.Charge = FixedPoint2.Max(FixedPoint2.Zero, component.Charge - totalDamage);

        if (component.Charge > FixedPoint2.Zero)
            return;

        component.Discharged = true;
        _itemToggle.TryDeactivate(uid);
    }

    private void OnInit(EntityUid uid, RechargeableComponent component, ComponentInit args)
    {
        component.Charge = component.MaxCharge;
    }
}
