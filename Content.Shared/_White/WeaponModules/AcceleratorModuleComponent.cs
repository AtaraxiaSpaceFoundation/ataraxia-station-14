namespace Content.Shared._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AcceleratorModuleComponent : BaseModuleComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float OldFireRate;
}
