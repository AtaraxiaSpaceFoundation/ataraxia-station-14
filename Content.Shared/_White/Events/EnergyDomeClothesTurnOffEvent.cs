using Content.Shared.Inventory;

namespace Content.Shared._White.Events;

public sealed class EnergyDomeClothesTurnOffEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.OUTERCLOTHING;
}
