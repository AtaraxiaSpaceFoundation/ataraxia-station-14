using Content.Shared.Atmos;

namespace Content.Server._White.ChangeTemperatureOnCollide;

[RegisterComponent]
public sealed partial class ChangeTemperatureOnCollideComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Temperature;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinTemperature = Atmospherics.TCMB;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxTemperature = 450;

    [DataField]
    public string FixtureID = "projectile";
}
