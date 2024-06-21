namespace Content.Shared._White.WeaponModules;

[RegisterComponent]
public sealed partial class AimModuleComponent : BaseModuleComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("divisor")]
    public float Divisor = 0.3F;
}
