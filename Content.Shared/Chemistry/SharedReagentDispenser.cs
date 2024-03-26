using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

/// <summary>
/// This class holds constants that are shared between client and server.
/// </summary>
public sealed class SharedReagentDispenser
{
    public const string OutputSlotName = "beakerSlot";
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount amount)
    : BoundUserInterfaceMessage
{
    public readonly ReagentDispenserDispenseAmount ReagentDispenserDispenseAmount = amount;
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserDispenseReagentMessage(ReagentId reagentId) : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId = reagentId;
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserClearContainerSolutionMessage : BoundUserInterfaceMessage;

public enum ReagentDispenserDispenseAmount
{
    U1 = 1,
    U5 = 5,
    U10 = 10,
    U15 = 15,
    U20 = 20,
    U25 = 25,
    U30 = 30,
    U50 = 50,
    U100 = 100,
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserBoundUserInterfaceState(
    ContainerInfo? outputContainer,
    NetEntity? outputContainerEntity,
    List<ReagentId> inventory,
    ReagentDispenserDispenseAmount selectedDispenseAmount)
    : BoundUserInterfaceState
{
    public readonly ContainerInfo? OutputContainer = outputContainer;

    public readonly NetEntity? OutputContainerEntity = outputContainerEntity;

    /// <summary>
    /// A list of the reagents which this dispenser can dispense.
    /// </summary>
    public readonly List<ReagentId> Inventory = inventory;

    public readonly ReagentDispenserDispenseAmount SelectedDispenseAmount = selectedDispenseAmount;
}

[Serializable, NetSerializable]
public enum ReagentDispenserUiKey
{
    Key
}