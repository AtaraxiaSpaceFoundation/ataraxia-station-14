using Content.Shared._White.WeaponModules;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
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

        SubscribeLocalEvent<Shared._White.WeaponModules.LightModuleComponent, EntGotInsertedIntoContainerMessage>(LightModuleOnInsert);
        SubscribeLocalEvent<Shared._White.WeaponModules.LightModuleComponent, EntGotRemovedFromContainerMessage>(LightModuleOnEject);

        SubscribeLocalEvent<Shared._White.WeaponModules.LaserModuleComponent, EntGotInsertedIntoContainerMessage>(LaserModuleOnInsert);
        SubscribeLocalEvent<Shared._White.WeaponModules.LaserModuleComponent, EntGotRemovedFromContainerMessage>(LaserModuleOnEject);

        SubscribeLocalEvent<Shared._White.WeaponModules.FlameHiderModuleComponent, EntGotInsertedIntoContainerMessage>(FlameHiderModuleOnInsert);
        SubscribeLocalEvent<Shared._White.WeaponModules.FlameHiderModuleComponent, EntGotRemovedFromContainerMessage>(FlameHiderModuleOnEject);

        SubscribeLocalEvent<SilencerModuleComponent, EntGotInsertedIntoContainerMessage>(SilencerModuleOnInsert);
        SubscribeLocalEvent<SilencerModuleComponent, EntGotRemovedFromContainerMessage>(SilencerModuleOnEject);

        SubscribeLocalEvent<Shared._White.WeaponModules.AcceleratorModuleComponent, EntGotInsertedIntoContainerMessage>(AcceleratorModuleOnInsert);
        SubscribeLocalEvent<Shared._White.WeaponModules.AcceleratorModuleComponent, EntGotRemovedFromContainerMessage>(AcceleratorModuleOnEject);
    }

    #region InsertModules
    private void LightModuleOnInsert(EntityUid module, Shared._White.WeaponModules.LightModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(!component.Modules.Contains(module))
            component.Modules.Add(module);

        EntityUid weapon = args.Container.Owner;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "light", appearanceComponent);

        _lightSystem.EnsureLight(weapon);

        _lightSystem.TryGetLight(weapon, out var light);
        _appearanceSystem.SetData(weapon, Modules.Light, "none", appearanceComponent);

        _lightSystem.SetRadius(weapon, 4F, light);
        _lightSystem.SetEnabled(weapon, true, light);
    }

    private void LaserModuleOnInsert(EntityUid module, Shared._White.WeaponModules.LaserModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(!component.Modules.Contains(module))
            component.Modules.Add(module);

        EntityUid weapon = args.Container.Owner;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "laser", appearanceComponent);
        _gunSystem.setProjectileSpeed(weapon, 35.5F);
    }

    private void FlameHiderModuleOnInsert(EntityUid module, Shared._White.WeaponModules.FlameHiderModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(!component.Modules.Contains(module))
            component.Modules.Add(module);

        EntityUid weapon = args.Container.Owner;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "flamehider", appearanceComponent);
        component.UseEffect = true;
        Dirty(module, component);
    }

    private void SilencerModuleOnInsert(EntityUid module, SilencerModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(!component.Modules.Contains(module))
            component.Modules.Add(module);

        EntityUid weapon = args.Container.Owner;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;
        if (!TryComp<GunComponent>(weapon, out var gunComp)) return;

        component.OldSoundGunshot = gunComp.SoundGunshot;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "silencer", appearanceComponent);
        component.UseEffect = true;
        _gunSystem.setSound(weapon, new SoundPathSpecifier("/Audio/White/Weapons/Modules/silence.ogg"));

        Dirty(module, component);
    }

    private void AcceleratorModuleOnInsert(EntityUid module, Shared._White.WeaponModules.AcceleratorModuleComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(!component.Modules.Contains(module))
            component.Modules.Add(module);

        EntityUid weapon = args.Container.Owner;

        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "accelerator", appearanceComponent);
        _gunSystem.setFireRate(weapon, 7.5F);
    }
    #endregion

    #region EjectModules
    private void LightModuleOnEject(EntityUid module, Shared._White.WeaponModules.LightModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(component.Modules.Contains(module))
            component.Modules.Remove(module);

        EntityUid weapon = args.Container.Owner;
        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        _lightSystem.TryGetLight(weapon, out var light);
        _lightSystem.SetEnabled(weapon, false, light);
    }

    private void LaserModuleOnEject(EntityUid module, Shared._White.WeaponModules.LaserModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(component.Modules.Contains(module))
            component.Modules.Remove(module);

        EntityUid weapon = args.Container.Owner;
        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        _gunSystem.setProjectileSpeed(weapon, 25F);
    }

    private void FlameHiderModuleOnEject(EntityUid module, Shared._White.WeaponModules.FlameHiderModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(component.Modules.Contains(module))
            component.Modules.Remove(module);

        EntityUid weapon = args.Container.Owner;
        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        component.UseEffect = false;
        Dirty(module, component);
    }

    private void SilencerModuleOnEject(EntityUid module, SilencerModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(component.Modules.Contains(module))
            component.Modules.Remove(module);

        EntityUid weapon = args.Container.Owner;
        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        component.UseEffect = false;
        _gunSystem.setSound(weapon, component.OldSoundGunshot!);
        Dirty(module, component);
    }

    private void AcceleratorModuleOnEject(EntityUid module, Shared._White.WeaponModules.AcceleratorModuleComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        if(component.Modules.Contains(module))
            component.Modules.Remove(module);

        EntityUid weapon = args.Container.Owner;
        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;

        _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
        _gunSystem.setFireRate(weapon, component.OldFireRate);
    }
    #endregion
}
