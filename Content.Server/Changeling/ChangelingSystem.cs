using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Examine;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ChemicalsSystem _chemicalsSystem = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implantSystem = default!;
    [Dependency] private readonly StoreSystem _storeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<AbsorbedComponent, ExaminedEvent>(OnExamine);

        InitializeAbilities();
        InitializeShop();
    }

#region Handlers

    private void OnInit(EntityUid uid, ChangelingComponent component, ComponentInit args)
    {
        SetupShop(uid, component);
        SetupInitActions(uid, component);
        CopyHumanoidData(uid, uid, component);

        _chemicalsSystem.UpdateAlert(uid, component);
        component.IsInited = true;
    }

    private void OnExamine(EntityUid uid, AbsorbedComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-juices-sucked-up"));
    }

#endregion

#region Helpers

    private void SetupShop(EntityUid uid, ChangelingComponent component)
    {
        if (component.IsInited)
            return;

        var coords = Transform(uid).Coordinates;
        var implant = Spawn("ChangelingShopImplant", coords);

        if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
            return;

        _implantSystem.ForceImplant(uid, implant, implantComp);

        if (!TryComp<StoreComponent>(implant, out var implantStore))
            return;

        implantStore.Balance.Add("ChangelingPoint", component.StartingPointsBalance);
    }

    private void SetupInitActions(EntityUid uid, ChangelingComponent component)
    {
        if (component.IsInited)
            return;

        _action.AddAction(uid, ChangelingAbsorb);
        _action.AddAction(uid, ChangelingTransform);
        _action.AddAction(uid, ChangelingRegenerate);
    }

#endregion
}
