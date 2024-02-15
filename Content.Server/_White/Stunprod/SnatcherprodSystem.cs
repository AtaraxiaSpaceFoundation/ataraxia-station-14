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

        EntityUid? heldEntity = null;

        if (hands.ActiveHandEntity != null)
            heldEntity = hands.ActiveHandEntity;
        else
        {
            foreach (var hand in hands.Hands)
            {
                if (hand.Value.HeldEntity == null)
                    continue;

                heldEntity = hand.Value.HeldEntity;
                break;
            }

            if (heldEntity == null)
                return;
        }

        if (!_hands.TryDrop(entity, heldEntity.Value, null, false, false, handsComp: hands))
            return;

        _hands.PickupOrDrop(args.User, heldEntity.Value, false);
    }
}
