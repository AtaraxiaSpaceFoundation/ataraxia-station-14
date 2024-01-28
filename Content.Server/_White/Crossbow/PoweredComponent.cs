using Content.Shared.Damage;

namespace Content.Server._White.Crossbow;

[RegisterComponent]
public sealed partial class PoweredComponent : Component
{
    [DataField("charge", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Charge;

    [DataField("damage", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();
}
