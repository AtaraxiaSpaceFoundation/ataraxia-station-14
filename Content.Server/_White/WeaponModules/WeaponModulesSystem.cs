using Content.Shared._White.WeaponModules;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._White.WeaponModules;

public sealed class WeaponModulesSystem : EntitySystem
{
    protected const string ModulesSlot = "gun_modules";
    [Dependency] private readonly PointLightSystem _lightSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightModuleComponent, EntGotInsertedIntoContainerMessage>(LightModuleOnInsert);
        SubscribeLocalEvent<LightModuleComponent, EntGotRemovedFromContainerMessage>(LightModuleOnEject);

        SubscribeLocalEvent<LaserModuleComponent, EntGotInsertedIntoContainerMessage>(LaserModuleOnInsert);
        SubscribeLocalEvent<LaserModuleComponent, EntGotRemovedFromContainerMessage>(LaserModuleOnEject);

        SubscribeLocalEvent<FlameHiderModuleComponent, EntGotInsertedIntoContainerMessage>(FlameHiderModuleOnInsert);
        SubscribeLocalEvent<FlameHiderModuleComponent, EntGotRemovedFromContainerMessage>(FlameHiderModuleOnEject);

        SubscribeLocalEvent<SilencerModuleComponent, EntGotInsertedIntoContainerMessage>(SilencerModuleOnInsert);
        SubscribeLocalEvent<SilencerModuleComponent, EntGotRemovedFromContainerMessage>(SilencerModuleOnEject);

        SubscribeLocalEvent<AcceleratorModuleComponent, EntGotInsertedIntoContainerMessage>(AcceleratorModuleOnInsert);
        SubscribeLocalEvent<AcceleratorModuleComponent, EntGotRemovedFromContainerMessage>(AcceleratorModuleOnEject);
    }

    private bool TryInsertModule(EntityUid module, EntityUid weapon, EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<WeaponModulesComponent>(weapon, out var weaponModulesComponent) && ModulesSlot == args.Container.ID)
        {
            if(!weaponModulesComponent.Modules.Contains(module))
                weaponModulesComponent.Modules.Add(module);
            return true;
        }

        return false;
    }

    private bool TryEjectModule(EntityUid module, EntityUid weapon, EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<WeaponModulesComponent>(weapon, out var weaponModulesComponent) && ModulesSlot == args.Container.ID)
        {
            if(weaponModulesComponent.Modules.Contains(module))
                weaponModulesComponent.Modules.Remove(module);
            return true;
        }

        return false;
    }

    #region InsertModules
    private void LightModuleOnInsert(EntityUid module, LightModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryInsertModule(module, weapon, args))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "light", appearanceComponent);

        _lightSystem.EnsureLight(weapon);

        _lightSystem.TryGetLight(weapon, out var light);
        _appearanceSystem.SetData(weapon, Modules.Light, "none", appearanceComponent);

        _lightSystem.SetRadius(weapon, 4F, light);
        _lightSystem.SetEnabled(weapon, true, light);
    }

    private void LaserModuleOnInsert(EntityUid module, LaserModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryInsertModule(module, weapon, args))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;
        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        component.OldProjectileSpeed = gunComp.ProjectileSpeed;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "laser", appearanceComponent);
        _gunSystem.SetProjectileSpeed(weapon, component.OldProjectileSpeed + 15F);
    }

    private void FlameHiderModuleOnInsert(EntityUid module, FlameHiderModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryInsertModule(module, weapon, args) || !TryComp<WeaponModulesComponent>(weapon, out var weaponModulesComponent))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "flamehider", appearanceComponent);
        weaponModulesComponent.UseEffect = true;
        Dirty(module, weaponModulesComponent);
    }

    private void SilencerModuleOnInsert(EntityUid module, SilencerModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryInsertModule(module, weapon, args) || !TryComp<WeaponModulesComponent>(weapon, out var weaponModulesComponent))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;
        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        component.OldSoundGunshot = gunComp.SoundGunshot;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "silencer", appearanceComponent);
        weaponModulesComponent.UseEffect = true;
        _gunSystem.SetSound(weapon, component.NewSoundGunshot);

        Dirty(module, weaponModulesComponent);
    }

    private void AcceleratorModuleOnInsert(EntityUid module, AcceleratorModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryInsertModule(module, weapon, args))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        component.OldFireRate = gunComp.FireRate;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "accelerator", appearanceComponent);
        _gunSystem.SetFireRate(weapon, component.OldFireRate + 2F);
    }
    #endregion

    #region EjectModules
    private void LightModuleOnEject(EntityUid module, LightModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        _lightSystem.TryGetLight(weapon, out var light);
        _lightSystem.SetRadius(weapon, 0F, light);
        _lightSystem.SetEnabled(weapon, false, light);
    }

    private void LaserModuleOnEject(EntityUid module, LaserModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        _gunSystem.SetProjectileSpeed(weapon, component.OldProjectileSpeed);
    }

    private void FlameHiderModuleOnEject(EntityUid module, FlameHiderModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args) || !TryComp<WeaponModulesComponent>(weapon, out var weaponModulesComponent))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        weaponModulesComponent.UseEffect = false;
        Dirty(module, weaponModulesComponent);
    }

    private void SilencerModuleOnEject(EntityUid module, SilencerModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args) || !TryComp<WeaponModulesComponent>(weapon, out var weaponModulesComponent))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        weaponModulesComponent.UseEffect = false;
        _gunSystem.SetSound(weapon, component.OldSoundGunshot!);
        Dirty(module, weaponModulesComponent);
    }

    private void AcceleratorModuleOnEject(EntityUid module, AcceleratorModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args))
            return;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        _gunSystem.SetFireRate(weapon, component.OldFireRate);

    }
    #endregion
}
