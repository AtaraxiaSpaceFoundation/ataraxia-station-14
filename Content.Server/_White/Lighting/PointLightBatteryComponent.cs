namespace Content.Server._White.Lighting;

[RegisterComponent]
public sealed partial class PointLightBatteryComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireBattery = true;
}
