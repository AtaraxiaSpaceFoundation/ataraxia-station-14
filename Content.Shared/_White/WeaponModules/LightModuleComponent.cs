namespace Content.Shared._White.WeaponModules;

/// <summary>
/// LightModuleComponent
/// </summary>
[RegisterComponent]
public sealed partial class LightModuleComponent : BaseModuleComponent
{
    public bool Enabled;

    public float Radius = 4F;
}
