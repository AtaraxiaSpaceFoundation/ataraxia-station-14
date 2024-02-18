namespace Content.Server._White.ChangeTemperatureOnCollide;

[RegisterComponent]
public sealed partial class ChangeTemperatureOnCollideComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Temperature;

    [DataField]
    public string FixtureID = "projectile";
}
