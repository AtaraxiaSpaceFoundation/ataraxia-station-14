namespace Content.Shared._White.WeaponModules;

/// <summary>
/// LightModuleComponent
/// </summary>
[RegisterComponent]
public sealed partial class LightModuleComponent : BaseModuleComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
}
