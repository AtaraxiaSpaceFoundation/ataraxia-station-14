using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NightVisionComponent : BaseEnhancedVisionComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public override Color Color { get; set; } = Color.FromHex("#98FB98");

    [DataField]
    public override EntProtoId? ToggleAction { get; set; } = "ToggleNightVision";
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent
{
}
