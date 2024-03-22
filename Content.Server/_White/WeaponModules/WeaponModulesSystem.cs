using System.Linq;
using Content.Client._White.WeaponsModules;
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

    SoundSpecifier? oldSoundGunshot;
    private float oldFireRate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponModulesComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<WeaponModulesComponent, EntRemovedFromContainerMessage>(OnEject);
    }

    private void OnInsert(EntityUid uid, WeaponModulesComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        EntityUid module = args.Entity;
        EntityUid weapon = args.Container.Owner;

        if (TryComp<GunComponent>(weapon, out var gunComp))
        {
            oldSoundGunshot = gunComp.SoundGunshot;
            oldFireRate = gunComp.FireRate;
        }

        InsertModules(module, comp);
        ModuleEffect(module, weapon);
    }

    private void OnEject(EntityUid uid, WeaponModulesComponent comp, EntRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        EntityUid module = args.Entity;
        EntityUid weapon = args.Container.Owner;

        RemoveModules(module, comp);
        RemoveModuleEffect(module, weapon);
    }

    private void InsertModules(EntityUid module, WeaponModulesComponent comp)
    {
        if(!comp.Modules.Contains(module))
            comp.Modules.Add(module);
    }

    private void ModuleEffect(EntityUid module, EntityUid weapon)
    {
        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;
        switch (module)
        {
            case "LightModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "light", appearanceComponent);

                _lightSystem.EnsureLight(weapon);

                _lightSystem.TryGetLight(weapon, out var light);
                _appearanceSystem.SetData(weapon, Modules.Light, "none", appearanceComponent);

                _lightSystem.SetRadius(weapon, 4F, light);
                _lightSystem.SetEnabled(weapon, true, light);
                break;

            case "LaserModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "laser", appearanceComponent);
                _gunSystem.setProjectileSpeed(weapon, 35.5F);
                break;

            case "FlameHiderModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "flamehider", appearanceComponent);
                _gunSystem.setUseEffect(weapon, true);
                break;

            case "SilencerModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "silencer", appearanceComponent);
                _gunSystem.setUseEffect(weapon, true);
                _gunSystem.setSound(weapon, new SoundPathSpecifier("/Audio/White/Weapons/Modules/silence.ogg"));
                break;

            case "AcceleratorModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "accelerator", appearanceComponent);
                _gunSystem.setFireRate(weapon, 7.5F);
                break;
        }
    }

    private void RemoveModules(EntityUid module, WeaponModulesComponent comp)
    {
        if(comp.Modules.Contains(module))
            comp.Modules.Remove(module);
    }

    private void RemoveModuleEffect(EntityUid module, EntityUid weapon)
    {
        if(!TryComp<AppearanceComponent>(weapon, out var appearanceComponent)) return;
        switch (module)
        {
            case "LightModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _lightSystem.TryGetLight(weapon, out var light);
                _lightSystem.SetEnabled(weapon, false, light);
                break;

            case "LaserModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _gunSystem.setProjectileSpeed(weapon, 25F);
                break;

            case "FlameHiderModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _gunSystem.setUseEffect(weapon, false);
                break;

            case "SilencerModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _gunSystem.setUseEffect(weapon, false);
                _gunSystem.setSound(weapon, oldSoundGunshot!);
                break;

            case "AcceleratorModule":
                _appearanceSystem.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _gunSystem.setFireRate(weapon, oldFireRate);
                break;
        }
    }
}
