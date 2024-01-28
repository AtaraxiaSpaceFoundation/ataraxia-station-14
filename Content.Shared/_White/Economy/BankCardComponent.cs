using Robust.Shared.GameStates;

namespace Content.Shared._White.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class BankCardComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public int? AccountId;

    [DataField("startingBalance")]
    public int StartingBalance;

    [DataField("commandBudgetCard")]
    public bool CommandBudgetCard;
}
