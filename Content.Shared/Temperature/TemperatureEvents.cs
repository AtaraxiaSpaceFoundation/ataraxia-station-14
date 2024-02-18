using Content.Shared.Inventory;

namespace Content.Shared.Temperature;

public sealed class ModifyChangedTemperatureEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public float TemperatureDelta;

    public ModifyChangedTemperatureEvent(float temperature)
    {
        TemperatureDelta = temperature;
    }
}

// WD START
public sealed class AdjustTemperatureEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public float Temperature;

    public AdjustTemperatureEvent(float temperature)
    {
        Temperature = temperature;
    }
}
// WD END
