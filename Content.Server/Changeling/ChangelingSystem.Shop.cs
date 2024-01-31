using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Changeling;
using Content.Shared.Implants.Components;
using Robust.Server.GameStates;
using Robust.Server.Placement;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem
{
    private void InitializeShop()
    {
        SubscribeLocalEvent<SubdermalImplantComponent,  ChangelingShopActionEvent>(OnShop);
    }

    private void OnShop(EntityUid uid, SubdermalImplantComponent component, ChangelingShopActionEvent args)
    {
        if(!TryComp<StoreComponent>(uid, out var store))
            return;

        _storeSystem.ToggleUi(args.Performer, uid, store);
    }
}
