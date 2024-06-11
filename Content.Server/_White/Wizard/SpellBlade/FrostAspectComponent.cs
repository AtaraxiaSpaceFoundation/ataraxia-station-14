using Content.Shared.Atmos;

namespace Content.Server._White.Wizard.SpellBlade;

[RegisterComponent]
public sealed partial class FrostAspectComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TemperatureOnHit = 100;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinTemperature = Atmospherics.TCMB;
}
