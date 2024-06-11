using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Cult.UI;

[Serializable, NetSerializable]
public sealed class CultTeleportSpellEuiState : EuiStateBase
{
    public Dictionary<int, string> Runes = new();
}

[Serializable, NetSerializable]
public sealed class TeleportSpellTargetRuneSelected : EuiMessageBase
{
    public int RuneUid;
}
