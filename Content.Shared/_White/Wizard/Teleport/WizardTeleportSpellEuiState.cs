using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Wizard.Teleport;

[Serializable, NetSerializable]
public sealed class WizardTeleportSpellEuiState : EuiStateBase
{
    public Dictionary<int, string> Locations = new();
}

[Serializable, NetSerializable]
public sealed class TeleportSpellTargetLocationSelected : EuiMessageBase
{
    public int LocationUid;
}
