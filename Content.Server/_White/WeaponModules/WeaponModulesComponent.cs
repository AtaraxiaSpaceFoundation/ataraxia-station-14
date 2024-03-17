namespace Content.Server._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class WeaponModulesComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string?> Modules = new();
}
