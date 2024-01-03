namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
public sealed partial class SurveillanceBodyCameraComponent : Component
{
    [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 0.3f;

    public bool lastState = false;
}
