using Content.Client.Weapons.Ranged.Components;
using Content.Shared._White.WeaponModules;
using Content.Shared.Rounding;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._White.WeaponsModules;

public sealed partial class WeaponModulesVisuals : VisualizerSystem<WeaponModulesComponent>
{
    [Dependency] private readonly PointLightSystem _lightSystem = default!;
    protected override void OnAppearanceChange(EntityUid uid, WeaponModulesComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if(args.Sprite == null)
            return;

        args.Sprite.LayerSetVisible(ModuleVisualState.HandGuardModule, false);
        args.Sprite.LayerSetVisible(ModuleVisualState.BarrelModule, false);

        if (AppearanceSystem.TryGetData<string>(uid, ModuleVisualState.HandGuardModule, out var handguardModule, args.Component) && handguardModule.Length != 0 && handguardModule != "none")
        {
            args.Sprite.LayerSetState(ModuleVisualState.HandGuardModule, handguardModule);
            args.Sprite.LayerSetVisible(ModuleVisualState.HandGuardModule, true);
        }

        if (AppearanceSystem.TryGetData<string>(uid, ModuleVisualState.BarrelModule, out var barrelModule, args.Component) && barrelModule.Length != 0 && barrelModule != "none")
        {
            args.Sprite.LayerSetState(ModuleVisualState.BarrelModule, barrelModule);
            args.Sprite.LayerSetVisible(ModuleVisualState.BarrelModule, true);
        }

        if (AppearanceSystem.TryGetData(uid, Modules.Light, out var data, args.Component))
        {
            if (TryComp<PointLightComponent>(uid, out var pointLightComponent))
            {
                if(!pointLightComponent.Enabled)
                    return;
                _lightSystem.SetMask("/Textures/White/Effects/LightMasks/lightModule.png", pointLightComponent!);
            }
        }
    }
}
