namespace Content.Server._White.Lighting.Pointlight.Battery;

[RegisterComponent]
public sealed partial class PointLightBatteryComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireBattery = true;
}
