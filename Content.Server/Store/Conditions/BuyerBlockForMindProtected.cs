using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

public sealed partial class BuyerBlockForMindProtected : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var roleSystem = ent.System<JobSystem>();
        var mindSystem = ent.System<MindSystem>();
        if (!mindSystem.TryGetMind(args.Buyer, out var mindId, out var mind))
            return false;
        if (mind.Session == null)
            return false;
        return !roleSystem.CanBeAntag(mind.Session);
    }
}
