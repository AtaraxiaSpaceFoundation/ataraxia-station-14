using Robust.Shared.GameStates;

namespace Content.Shared._White.BuffedFlashGrenade;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlashSoundSuppressionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxRange = 3f;
}
