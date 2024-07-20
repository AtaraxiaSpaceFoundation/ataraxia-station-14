using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThermalVisionComponent : BaseNvOverlayComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public override Color Color { get; set; } = Color.FromHex("#F84742");

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IsActive = true;

    [DataField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/White/Items/Goggles/activate.ogg");

    [DataField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/White/Items/Goggles/deactivate.ogg");

    [DataField]
    public EntProtoId? ToggleAction = "ToggleThermalVision";

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}

public sealed partial class ToggleThermalVisionEvent : InstantActionEvent
{
}
