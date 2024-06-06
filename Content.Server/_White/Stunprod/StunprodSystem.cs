using System.Diagnostics.CodeAnalysis;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;

namespace Content.Server._White.Stunprod;

public sealed class StunprodSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    private const string CellSlot = "cell_slot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunprodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StunprodComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        SubscribeLocalEvent<StunprodComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<StunprodComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<StunprodComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
        SubscribeLocalEvent<StunprodComponent, ItemToggledEvent>(ToggleDone);
        SubscribeLocalEvent<StunprodComponent, ComponentStartup>(OnStartup,
            after: new[] {typeof(SharedItemToggleSystem)});
    }

    private void OnStartup(Entity<StunprodComponent> ent, ref ComponentStartup args)
    {
        _appearance.SetData(ent, ToggleVisuals.Toggled, "nocell");
    }

    private void OnEntInserted(EntityUid uid, StunprodComponent component, EntInsertedIntoContainerMessage args)
    {
        _itemToggle.TryDeactivate(uid, predicted: false);

        _appearance.SetData(uid, ToggleVisuals.Toggled, false);
    }

    private void OnEntRemoved(EntityUid uid, StunprodComponent component, EntRemovedFromContainerMessage args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        _itemToggle.TryDeactivate(uid, predicted: false);

        _appearance.SetData(uid, ToggleVisuals.Toggled, "nocell");
    }

    private void OnStaminaHitAttempt(EntityUid uid, StunprodComponent component,
        ref StaminaDamageOnHitAttemptEvent args)
    {
        if (!_itemToggle.IsActivated(uid) || !TryGetBatteryComponent(uid, out var battery, out var batteryUid) ||
            !_battery.TryUseCharge(batteryUid.Value, component.EnergyPerUse, battery))
        {
            args.Cancelled = true;
            return;
        }

        if (battery.CurrentCharge < component.EnergyPerUse)
            _itemToggle.TryDeactivate(uid, predicted: false);
    }

    private void OnExamined(EntityUid uid, StunprodComponent comp, ExaminedEvent args)
    {
        var msg = _itemToggle.IsActivated(uid)
            ? Loc.GetString("comp-stunprod-examined-on")
            : Loc.GetString("comp-stunprod-examined-off");
        args.PushMarkup(msg);
    }

    private void ToggleDone(Entity<StunprodComponent> entity, ref ItemToggledEvent args)
    {
        if (entity.Comp.HasHeldPrefix && TryComp<ItemComponent>(entity, out var item))
            _item.SetHeldPrefix(entity.Owner, args.Activated ? "on" : "off", component: item);

        if (!TryGetBatteryComponent(entity, out _, out _))
            _appearance.SetData(entity, ToggleVisuals.Toggled, "nocell");
    }

    private void TryTurnOn(Entity<StunprodComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (TryGetBatteryComponent(entity, out var battery, out _) &&
            battery.CurrentCharge >= entity.Comp.EnergyPerUse)
            return;

        args.Cancelled = true;

        _appearance.SetData(entity, ToggleVisuals.Toggled, battery == null ? "nocell" : false);

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
