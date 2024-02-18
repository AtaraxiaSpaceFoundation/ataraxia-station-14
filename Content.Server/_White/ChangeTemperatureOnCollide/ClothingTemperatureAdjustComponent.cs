namespace Content.Server._White.ChangeTemperatureOnCollide;

[RegisterComponent]
public sealed partial class ClothingTemperatureAdjustComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Rate = 1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemperature = 310.15f;
}
