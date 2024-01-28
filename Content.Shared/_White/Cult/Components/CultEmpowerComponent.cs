using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Cult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultEmpowerComponent : Component
{
    [DataField("isRune")]
    public bool IsRune;

    public int MaxAllowedCultistActions = 4;
    public int MinRequiredCultistActions = 1;
}

[Serializable, NetSerializable]
public sealed class CultEmpowerSelectedBuiMessage : BoundUserInterfaceMessage
{
    public string ActionType;

    public CultEmpowerSelectedBuiMessage(string actionType)
    {
        ActionType = actionType;
    }
}

[Serializable, NetSerializable]
public enum CultEmpowerUiKey : byte
{
    Key
}
