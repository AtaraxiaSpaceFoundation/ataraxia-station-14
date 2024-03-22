using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._White.WeaponModules;

public abstract class SharedWeaponModulesSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponModulesComponent, GunMuzzleFlashAttemptEvent>(OnMuzzleFlashEvent);
    }

    private void OnMuzzleFlashEvent(EntityUid weapon, WeaponModulesComponent component, ref GunMuzzleFlashAttemptEvent args)
    {
        args.Cancelled = component.UseEffect;
    }
}
