using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

public sealed partial class BuyerBlockForAntagCondition : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var roleSystem = ent.System<RoleSystem>();
        var mindSystem = ent.System<MindSystem>();
        if (!mindSystem.TryGetMind(args.Buyer, out var mindId, out var mind))
            return false;
        return !roleSystem.MindIsAntagonist(mindId);
    }
}
