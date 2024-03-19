using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Rounding;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._White.WeaponsModules;

public sealed partial class WeaponModulesVisuals : EntitySystem
{
    private void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponModulesVisualsComponent, ComponentInit>(ComponentInit);
        SubscribeLocalEvent<WeaponModulesVisualsComponent, AppearanceChangeEvent>(onModuleVisualChange);
    }

    private void ComponentInit(EntityUid uid, WeaponModulesVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)) return;

        if (sprite.LayerMapTryGet(ModuleVisualState.Laser, out _))
        {
            sprite.LayerSetState(ModuleVisualState.Laser, $"laser");
            sprite.LayerSetVisible(ModuleVisualState.Laser, false);
        }
    }

    private void onModuleVisualChange(EntityUid uid, WeaponModulesVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null) return;

        if (sprite.LayerMapTryGet(ModuleVisualState.Laser, out _))
        {
            sprite.LayerSetVisible(ModuleVisualState.Laser, true);
            sprite.LayerSetState(ModuleVisualState.Laser, $"laser");
        }
    }

}
