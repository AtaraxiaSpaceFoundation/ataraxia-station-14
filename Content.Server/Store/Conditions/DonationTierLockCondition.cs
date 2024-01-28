using Content.Server._White.Sponsors;
using Content.Shared.Store;
using Robust.Shared.Player;

namespace Content.Server.Store.Conditions;

public sealed partial class DonationTierLockCondition : ListingCondition
{
    [DataField("tier", required: true)]
    public int Tier;
    public override bool Condition(ListingConditionArgs args)
    {
        var entityManager = args.EntityManager;
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        if(!entityManager.TryGetComponent<ActorComponent>(args.Buyer, out var actor)) return false;

        if(!sponsorsManager.TryGetInfo(actor.PlayerSession.UserId, out var sponsorInfo)) return false;

        if (sponsorInfo.Tier != Tier) return false;

        return true;
    }
}
