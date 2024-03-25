namespace Content.Shared._White.WeaponModules;

/// <summary>
/// LightModuleComponent
/// </summary>
[RegisterComponent]
public sealed partial class LightModuleComponent : BaseModuleComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool Enabled;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float Radius = 4F;
}
