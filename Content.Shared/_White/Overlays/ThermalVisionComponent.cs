using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThermalVisionComponent : BaseEnhancedVisionComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public override Color Color { get; set; } = Color.FromHex("#F84742");

    [DataField]
    public override EntProtoId? ToggleAction { get; set; } = "ToggleThermalVision";
}

public sealed partial class ToggleThermalVisionEvent : InstantActionEvent
{
}
