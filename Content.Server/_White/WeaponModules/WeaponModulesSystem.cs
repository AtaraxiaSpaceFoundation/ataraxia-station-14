using Content.Server.Light.Events;
using Content.Server.Lightning;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._White.WeaponModules;

public sealed class WeaponModulesSystem : EntitySystem
{
    protected const string ModulesSlot = "gun_modules";
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
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

        string weapon = Prototype(args.Entity)!.ID;
        EntityUid target = args.Container.Owner;

        insertModules(weapon, comp);
        moduleEffect(weapon, target);
    }

    private void OnEject(EntityUid uid, WeaponModulesComponent comp, EntRemovedFromContainerMessage args)
    {
        if (ModulesSlot != args.Container.ID)
            return;

        string weapon = Prototype(args.Entity)!.ID;
        EntityUid target = args.Container.Owner;

        removeModules(weapon, comp);
        removeModuleEffect(weapon, target);
    }

    private void insertModules(string module, WeaponModulesComponent comp)
    {
        switch (module)
        {
            case "LightModule":
                if(comp.Modules.Contains("LightModule")) break;
                comp.Modules.Add("LightModule");
                break;
        }
    }

    private void moduleEffect(string module, EntityUid target)
    {
        switch (module)
        {
            case "LightModule":
                if (HasComp<PointLightComponent>(target))
                {
                    _lightSystem.SetEnabled(target, true);
                        break;
                }

                _lightSystem.TryGetLight(target, out var light);
                _lightSystem.EnsureLight(target);
                _lightSystem.SetRadius(target, 2F, light);
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
        }
    }

    private void removeModuleEffect(string module, EntityUid target)
    {
        switch (module)
        {
            case "LightModule":
                if(!HasComp<PointLightComponent>(target))
                    break;

                _lightSystem.TryGetLight(target, out var light);
                _lightSystem.SetEnabled(target, false, light);
                break;
        }
    }
}
