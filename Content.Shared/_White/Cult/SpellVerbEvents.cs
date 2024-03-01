using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Cult;

[Serializable, NetSerializable]
public sealed partial class SpellCreatedEvent : SimpleDoAfterEvent
{
    public string Spell = "";
}

[Serializable, NetSerializable]
public sealed class CultEmpowerRemoveBuiMessage : BoundUserInterfaceMessage
{
    public NetEntity ActionType;

    public CultEmpowerRemoveBuiMessage(NetEntity actionType)
    {
        ActionType = actionType;
    }
}

[Serializable, NetSerializable]
public enum CultEmpowerRemoveUiKey : byte
{
    Key
}
