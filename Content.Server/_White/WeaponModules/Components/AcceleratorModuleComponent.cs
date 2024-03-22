namespace Content.Server._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AcceleratorModuleComponent : Shared._White.WeaponModules.WeaponModulesComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float OldFireRate;
}
