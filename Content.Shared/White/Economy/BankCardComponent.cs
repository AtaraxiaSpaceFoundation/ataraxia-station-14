﻿using Robust.Shared.GameStates;

namespace Content.Shared.White.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class BankCardComponent : Component
{
    [DataField("accountId")]
    public int? BankAccountId;

    [DataField("startingBalance")]
    public int StartingBalance = 0;

    [DataField("commandBudgetCard")]
    public bool CommandBudgetCard;
}
