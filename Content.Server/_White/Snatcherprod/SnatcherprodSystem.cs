using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;

namespace Content.Server._White.Snatcherprod;

public sealed class SnatcherprodSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;

    private const string CellSlot = "cell_slot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnatcherprodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SnatcherprodComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        SubscribeLocalEvent<SnatcherprodComponent, StaminaMeleeHitEvent>(OnHit);
        SubscribeLocalEvent<SnatcherprodComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<SnatcherprodComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<SnatcherprodComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
        SubscribeLocalEvent<SnatcherprodComponent, ItemToggledEvent>(ToggleDone);
    }

    private void OnEntInserted(EntityUid uid, SnatcherprodComponent component, EntInsertedIntoContainerMessage args)
    {
        _itemToggle.TryDeactivate(uid, predicted: false);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, ToggleVisuals.Toggled, false, appearance);
        }
    }

    private void OnEntRemoved(EntityUid uid, SnatcherprodComponent component, EntRemovedFromContainerMessage args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        if (!_itemToggle.IsActivated(uid))
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                _appearance.SetData(uid, ToggleVisuals.Toggled, "nocell", appearance);
            }
        }
        else
            _itemToggle.TryDeactivate(uid, predicted: false);
    }

    private void OnHit(EntityUid uid, SnatcherprodComponent component, StaminaMeleeHitEvent args)
    {
        if (!_itemToggle.IsActivated(uid) || args.HitList.Count == 0)
            return;

        var entity = args.HitList.First().Entity;

        if (!TryComp(entity, out HandsComponent? hands))
            return;

        EntityUid? heldEntity = null;

        if (hands.ActiveHandEntity != null)
            heldEntity = hands.ActiveHandEntity;
        else
        {
            foreach (var hand in hands.Hands)
            {
                if (hand.Value.HeldEntity == null)
                    continue;

                heldEntity = hand.Value.HeldEntity;
                break;
            }

            if (heldEntity == null)
                return;
        }

        if (!_hands.TryDrop(entity, heldEntity.Value, null, false, false, handsComp: hands))
            return;

        _hands.PickupOrDrop(args.User, heldEntity.Value, false);
    }

    private void OnStaminaHitAttempt(EntityUid uid, SnatcherprodComponent component, ref StaminaDamageOnHitAttemptEvent args)
    {
        if (!_itemToggle.IsActivated(uid) || !TryGetBatteryComponent(uid, out var battery, out var batteryUid) ||
            !_battery.TryUseCharge(batteryUid.Value, component.EnergyPerUse, battery))
        {
            args.Cancelled = true;
            return;
        }

        if (battery.CurrentCharge < component.EnergyPerUse)
        {
            _itemToggle.TryDeactivate(uid, predicted: false);
        }
    }

    private void OnExamined(EntityUid uid, SnatcherprodComponent comp, ExaminedEvent args)
    {
        var msg = _itemToggle.IsActivated(uid)
            ? Loc.GetString("comp-snatcherprod-examined-on")
            : Loc.GetString("comp-snatcherprod-examined-off");
        args.PushMarkup(msg);
    }

    private void ToggleDone(Entity<SnatcherprodComponent> entity, ref ItemToggledEvent args)
    {
        if (TryGetBatteryComponent(entity, out _, out _) || !TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        _appearance.SetData(entity, ToggleVisuals.Toggled, "nocell", appearance);
    }

    private void TryTurnOn(Entity<SnatcherprodComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (TryGetBatteryComponent(entity, out var battery, out _) &&
            battery.CurrentCharge >= entity.Comp.EnergyPerUse)
            return;

        args.Cancelled = true;

        if (TryComp<AppearanceComponent>(entity, out var appearance))
        {
            _appearance.SetData(entity, ToggleVisuals.Toggled, battery == null ? "nocell" : false, appearance);
        }

        if (args.User != null)
        {
            _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid) args.User,
                (EntityUid) args.User);
        }
    }

    private bool TryGetBatteryComponent(EntityUid uid, [NotNullWhen(true)] out BatteryComponent? battery,
        [NotNullWhen(true)] out EntityUid? batteryUid)
    {
        if (TryComp(uid, out battery))
        {
            batteryUid = uid;
            return true;
        }

        if (!_containers.TryGetContainer(uid, CellSlot, out var container) ||
            container is not ContainerSlot slot)
        {
            battery = null;
            batteryUid = null;
            return false;
        }

        batteryUid = slot.ContainedEntity;

        if (batteryUid != null)
            return TryComp(batteryUid, out battery);

        battery = null;
        return false;
    }
}
