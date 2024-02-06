using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NightVisionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector3 Tint = new(0.3f, 0.3f, 0.3f);

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Strength = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Noise = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color = Color.FromHex("#98FB98");

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IsActive = true;

    [DataField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Items/flashlight_pda.ogg");

    [DataField]
    public EntProtoId? ToggleAction = "ToggleNightVision";

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent
{
}
