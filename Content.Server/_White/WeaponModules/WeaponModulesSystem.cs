using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Telescope;
using Content.Shared._White.WeaponModules;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._White.WeaponModules;

public sealed class WeaponModulesSystem : EntitySystem
{
    protected static readonly Dictionary<string, Enum> Slots = new()
    {
        { "handguard_module", ModuleVisualState.HandGuardModule }, { "barrel_module", ModuleVisualState.BarrelModule }, { "aim_module", ModuleVisualState.AimModule }
    };

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

        SubscribeLocalEvent<AimModuleComponent, EntGotInsertedIntoContainerMessage>(EightAimModuleOnInsert);
        SubscribeLocalEvent<AimModuleComponent, EntGotRemovedFromContainerMessage>(EightAimModuleOnEject);
    }

    private bool TryInsertModule(EntityUid module, EntityUid weapon, BaseModuleComponent component,
        string containerId, [NotNullWhen(true)] out WeaponModulesComponent? weaponModulesComponent)
    {
        if (!TryComp(weapon, out weaponModulesComponent) || !TryComp<AppearanceComponent>(weapon, out var appearanceComponent) ||
            !Slots.ContainsKey(containerId))
        {
            weaponModulesComponent = null;
            appearanceComponent = null;
            return false;
        }

        if(!weaponModulesComponent.Modules.Contains(module))
            weaponModulesComponent.Modules.Add(module);

        if (!Slots.TryGetValue(containerId, out var value))
            return false;

        _appearanceSystem.SetData(weapon, value, component.AppearanceValue, appearanceComponent);

        return true;
    }

    private bool TryEjectModule(EntityUid module, EntityUid weapon, string containerId, [NotNullWhen(true)] out WeaponModulesComponent? weaponModulesComponent)
    {
        if (!TryComp(weapon, out weaponModulesComponent) || !TryComp<AppearanceComponent>(weapon, out var appearanceComponent) || !Slots.ContainsKey(containerId))
        {
            weaponModulesComponent = null;
            appearanceComponent = null;
            return false;
        }


        if(weaponModulesComponent.Modules.Contains(module))
            weaponModulesComponent.Modules.Remove(module);

        if (!Slots.TryGetValue(containerId, out var value))
            return false;

        _appearanceSystem.SetData(weapon, value, "none", appearanceComponent);

        return true;
    }

    #region InsertModules
    private void LightModuleOnInsert(EntityUid module, LightModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryInsertModule(module, weapon, component, args.Container.ID, out var weaponModulesComponent))
            return;

        TryComp<AppearanceComponent>(weapon, out var appearanceComponent);

        SharedPointLightComponent light = _lightSystem.EnsureLight(weapon);

        _appearanceSystem.SetData(weapon, Modules.Light, "none", appearanceComponent);

        _lightSystem.SetRadius(weapon, component.Radius, light);
        _lightSystem.SetEnabled(weapon, true, light);
    }

    private void LaserModuleOnInsert(EntityUid module, LaserModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        if(!TryInsertModule(module, weapon, component, args.Container.ID, out var weaponModulesComponent))
            return;

        component.OldProjectileSpeed = gunComp.ProjectileSpeed;

        _gunSystem.SetProjectileSpeed(weapon, component.OldProjectileSpeed + component.ProjectileSpeedAdd);
    }

    private void FlameHiderModuleOnInsert(EntityUid module, FlameHiderModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryInsertModule(module, weapon, component, args.Container.ID, out var weaponModulesComponent))
            return;

        weaponModulesComponent.WeaponFireEffect = true;
        Dirty(module, weaponModulesComponent);
    }

    private void SilencerModuleOnInsert(EntityUid module, SilencerModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        if(!TryInsertModule(module, weapon, component, args.Container.ID, out var weaponModulesComponent))
            return;

        component.OldSoundGunshot = gunComp.SoundGunshot;

        weaponModulesComponent.WeaponFireEffect = true;
        _gunSystem.SetSound(weapon, component.NewSoundGunshot);

        Dirty(module, weaponModulesComponent);
    }

    private void AcceleratorModuleOnInsert(EntityUid module, AcceleratorModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        if(!TryInsertModule(module, weapon, component, args.Container.ID, out var weaponModulesComponent))
            return;

        component.OldFireRate = gunComp.FireRate;

        _gunSystem.SetFireRate(weapon, component.OldFireRate + component.FireRateAdd);
    }

    private void EightAimModuleOnInsert(EntityUid module, AimModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        if(!TryInsertModule(module, weapon, component, args.Container.ID, out var weaponModulesComponent))
            return;

        EnsureComp<TelescopeComponent>(weapon).Divisor = component.Divisor;
    }
    #endregion

    #region EjectModules
    private void LightModuleOnEject(EntityUid module, LightModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args.Container.ID, out var weaponModulesComponent))
            return;

        if(!_lightSystem.TryGetLight(weapon, out var light))
            return;

        _lightSystem.SetRadius(weapon, 0F, light);
        _lightSystem.SetEnabled(weapon, false, light);
    }

    private void LaserModuleOnEject(EntityUid module, LaserModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args.Container.ID, out var weaponModulesComponent))
            return;

        _gunSystem.SetProjectileSpeed(weapon, component.OldProjectileSpeed);
    }

    private void FlameHiderModuleOnEject(EntityUid module, FlameHiderModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args.Container.ID, out var weaponModulesComponent))
            return;

        weaponModulesComponent.WeaponFireEffect = false;
        Dirty(module, weaponModulesComponent);
    }

    private void SilencerModuleOnEject(EntityUid module, SilencerModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args.Container.ID, out var weaponModulesComponent))
            return;

        weaponModulesComponent.WeaponFireEffect = false;
        _gunSystem.SetSound(weapon, component.OldSoundGunshot!);
        Dirty(module, weaponModulesComponent);
    }

    private void AcceleratorModuleOnEject(EntityUid module, AcceleratorModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args.Container.ID, out var weaponModulesComponent))
            return;

        _gunSystem.SetFireRate(weapon, component.OldFireRate);
    }

    private void EightAimModuleOnEject(EntityUid module, AimModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityUid weapon = args.Container.Owner;

        if(!TryEjectModule(module, weapon, args.Container.ID, out var weaponModulesComponent))
            return;

        RemComp<TelescopeComponent>(weapon);
    }
    #endregion
}
