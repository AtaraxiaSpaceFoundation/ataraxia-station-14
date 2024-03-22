namespace Content.Server._White.WeaponModules;

/// <summary>
/// Base Module Component
/// </summary>
[RegisterComponent]
public partial class WeaponModulesComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public List<EntityUid> Modules = new();
}
