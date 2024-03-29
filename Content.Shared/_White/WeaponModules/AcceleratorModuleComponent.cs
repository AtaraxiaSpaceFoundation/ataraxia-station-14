namespace Content.Shared._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AcceleratorModuleComponent : BaseModuleComponent
{
    public float OldFireRate;

    public float FireRateAdd = 2.4F;
}
