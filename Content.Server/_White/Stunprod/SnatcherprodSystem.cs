using System.Linq;
using Content.Shared.Damage.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle;

namespace Content.Server._White.Stunprod;

public sealed class SnatcherprodSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnatcherprodComponent, StaminaMeleeHitEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, SnatcherprodComponent component, StaminaMeleeHitEvent args)
    {
        if (!_itemToggle.IsActivated(uid) || args.HitList.Count == 0)
            return;

        var entity = args.HitList.First().Entity;

        if (entity == uid || !TryComp(entity, out HandsComponent? hands))
            return;

        foreach (var heldEntity in _handsSystem.EnumerateHeld(entity, hands))
        {
            if (!_hands.TryDrop(entity, heldEntity, null, false, false, handsComp: hands))
                continue;

            _hands.PickupOrDrop(args.User, heldEntity, false);
            break;
        }
    }
}
