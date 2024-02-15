namespace Content.Server._White.Stunprod;

[RegisterComponent]
public sealed partial class StunprodComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyPerUse { get; set; } = 72;

    [DataField]
    public bool HasHeldPrefix = true;
}
