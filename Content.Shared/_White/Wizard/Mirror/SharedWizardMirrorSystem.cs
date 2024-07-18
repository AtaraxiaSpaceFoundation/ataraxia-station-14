using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Wizard.Mirror;

[Serializable, NetSerializable]
public enum WizardMirrorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class WizardMirrorSave(HumanoidCharacterProfile profile) : BoundUserInterfaceMessage
{
    public HumanoidCharacterProfile Profile { get; } = profile;
}

[Serializable, NetSerializable]
public sealed class WizardMirrorUiState(
    HumanoidCharacterProfile profile)
    : BoundUserInterfaceState
{
    public NetEntity Target;

    public HumanoidCharacterProfile Profile = profile;
}
