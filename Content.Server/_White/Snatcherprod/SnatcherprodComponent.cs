namespace Content.Server._White.Snatcherprod;

[RegisterComponent, Access(typeof(SnatcherprodSystem))]
public sealed partial class SnatcherprodComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("energyPerUse")]
    public float EnergyPerUse { get; set; } = 36;
}
