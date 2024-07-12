using Robust.Shared.Prototypes;

namespace Content.Server._White.SurveillanceCamera;

[RegisterComponent]
public sealed partial class SurveillanceBodyCameraComponent : Component
{
    [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 0.3f;

    [DataField]
    public EntityUid? ToggleActionEntity;

    [DataField]
    public EntProtoId ToggleAction = "ToggleBodyCamera";

    public bool LastState = false;
}

