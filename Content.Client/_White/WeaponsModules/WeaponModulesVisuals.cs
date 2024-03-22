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

        args.Sprite.LayerSetVisible(ModuleVisualState.Module, false);

        if (AppearanceSystem.TryGetData<string>(uid, ModuleVisualState.Module, out var module, args.Component) && module.Length != 0 && module != "none")
        {
            args.Sprite.LayerSetState(ModuleVisualState.Module, module);
            args.Sprite.LayerSetVisible(ModuleVisualState.Module, true);
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
