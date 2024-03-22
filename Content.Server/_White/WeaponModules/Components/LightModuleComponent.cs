namespace Content.Server._White.WeaponModules;

/// <summary>
/// LightModuleComponent
/// </summary>
[RegisterComponent]
public sealed partial class LightModuleComponent : WeaponModulesComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
}
