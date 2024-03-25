namespace Content.Shared._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class LaserModuleComponent : BaseModuleComponent
{
    public float OldProjectileSpeed;

    public float ProjectileSpeedAdd = 15F;
}
