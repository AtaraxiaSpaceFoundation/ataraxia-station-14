using Content.Shared.Ghost;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared._White.Cult.Components;

namespace Content.Shared._White.Cult.Systems;

public sealed class CultItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultItemComponent, GettingPickedUpAttemptEvent>(OnHandPickUp);
    }

    private void OnHandPickUp(EntityUid uid, CultItemComponent component, GettingPickedUpAttemptEvent args)
    {
        if (HasComp<Components.CultistComponent>(args.User) || HasComp<GhostComponent>(args.User))
            return;

        args.Cancel();
        _transform.AttachToGridOrMap(uid);
        _popupSystem.PopupClient(Loc.GetString("cult-item-component-not-cultist", ("name", Name(uid))), uid, args.User);
    }
}
