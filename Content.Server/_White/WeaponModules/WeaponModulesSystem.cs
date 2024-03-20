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
    [Dependency] private readonly SharedAppearanceSystem Appearance = default!;
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

        string module = Prototype(args.Entity)!.ID;
        EntityUid weapon = args.Container.Owner;

        TryComp<GunComponent>(weapon, out var gunComp);
        oldSoundGunshot = gunComp!.SoundGunshot;
        oldFireRate = gunComp.FireRate;

        insertModules(module, comp);
        moduleEffect(module, weapon);
    }

    private void OnEject(EntityUid uid, WeaponModulesComponent comp, EntRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        string module = Prototype(args.Entity)!.ID;
        EntityUid weapon = args.Container.Owner;

        removeModules(module, comp);
        removeModuleEffect(module, weapon);
    }

    private void insertModules(string module, WeaponModulesComponent comp)
    {
        switch (module)
        {
            case "LightModule":
                if(comp.Modules.Contains("LightModule")) break;
                comp.Modules.Add("LightModule");
                break;

            case "LaserModule":
                if(comp.Modules.Contains("LaserModule")) break;
                comp.Modules.Add("LaserModule");
                break;

            case "FlameHiderModule":
                if(comp.Modules.Contains("FlameHiderModule")) break;
                comp.Modules.Add("FlameHiderModule");
                break;

            case "SilencerModule":
                if(comp.Modules.Contains("SilencerModule")) break;
                comp.Modules.Add("SilencerModule");
                break;

            case "AcceleratorModule":
                if(comp.Modules.Contains("AcceleratorModule")) break;
                comp.Modules.Add("AcceleratorModule");
                break;
        }
    }

    private void moduleEffect(string module, EntityUid weapon)
    {
        TryComp<AppearanceComponent>(weapon, out var appearanceComponent);
        switch (module)
        {
            case "LightModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "light", appearanceComponent);

                _lightSystem.EnsureLight(weapon);

                _lightSystem.TryGetLight(weapon, out var light);
                Appearance.SetData(weapon, Modules.Light, "none", appearanceComponent);

                _lightSystem.SetRadius(weapon, 4F, light);
                _lightSystem.SetEnabled(weapon, true, light);
                break;

            case "LaserModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "laser", appearanceComponent);
                _gunSystem.setProjectileSpeed(weapon, 35.5F);
                break;

            case "FlameHiderModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "flamehider", appearanceComponent);
                _gunSystem.setUseEffect(weapon, true);
                break;

            case "SilencerModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "silencer", appearanceComponent);
                _gunSystem.setUseEffect(weapon, true);
                _gunSystem.setSound(weapon, new SoundPathSpecifier("/Audio/White/Weapons/Modules/silence.ogg"));
                break;

            case "AcceleratorModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "accelerator", appearanceComponent);
                _gunSystem.setFireRate(weapon, 7.5F);
                break;
        }
    }

    private void removeModules(string module, WeaponModulesComponent comp)
    {
        switch (module)
        {
            case "LightModule":
                comp.Modules.Remove("LightModule");
                break;

            case "LaserModule":
                comp.Modules.Remove("LaserModule");
                break;

            case "FlameHiderModule":
                comp.Modules.Remove("FlameHiderModule");
                break;

            case "SilencerModule":
                comp.Modules.Remove("SilencerModule");
                break;

            case "AcceleratorModule":
                comp.Modules.Remove("AcceleratorModule");
                break;
        }
    }

    private void removeModuleEffect(string module, EntityUid weapon)
    {
        TryComp<AppearanceComponent>(weapon, out var appearanceComponent);
        switch (module)
        {
            case "LightModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _lightSystem.TryGetLight(weapon, out var light);
                _lightSystem.SetEnabled(weapon, false, light);
                break;

            case "LaserModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                if (!HasComp<GunComponent>(weapon))
                    break;
                _gunSystem.setProjectileSpeed(weapon, 25F);
                break;

            case "FlameHiderModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _gunSystem.setUseEffect(weapon, false);
                break;

            case "SilencerModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _gunSystem.setUseEffect(weapon, false);
                _gunSystem.setSound(weapon, oldSoundGunshot!);
                break;

            case "AcceleratorModule":
                Appearance.SetData(weapon, ModuleVisualState.Module, "none", appearanceComponent);
                _gunSystem.setFireRate(weapon, oldFireRate);
                break;
        }
    }
}
