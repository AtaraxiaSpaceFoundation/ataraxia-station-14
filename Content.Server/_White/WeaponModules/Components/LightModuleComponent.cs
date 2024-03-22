namespace Content.Server._White.WeaponModules;

/// <summary>
/// LightModuleComponent
/// </summary>
[RegisterComponent]
public sealed partial class LightModuleComponent : Shared._White.WeaponModules.WeaponModulesComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
}
