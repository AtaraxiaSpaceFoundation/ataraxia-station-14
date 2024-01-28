/*using System.Diagnostics;
using System.Linq;
using Content.Server.Mind;
using Content.Server._White.Cult.GameRule;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed partial class KillCultistTarget : IObjectiveCondition
{
    private IEntityManager EntityManager => IoCManager.Resolve<IEntityManager>();

    protected EntityUid? TargetMindId;
    protected MindComponent? TargetMind => EntityManager.GetComponentOrNull<MindComponent>(TargetMindId);

    protected SharedJobSystem Jobs => EntityManager.System<SharedJobSystem>();


    public IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
    {
        var cultistRule = EntityManager.EntityQuery<CultRuleComponent>().FirstOrDefault();
        Debug.Assert(cultistRule != null, nameof(cultistRule) + " != null");
        var target = cultistRule.CultTarget;

        return new KillCultistTarget()
        {
            TargetMindId = target
        };
    }

    public string Title
    {
        get
        {
            var targetName = string.Empty;
            var jobName = Jobs.MindTryGetJobName(TargetMindId) ?? "Unknown";

            if (TargetMindId == null)
                return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName), ("job", jobName));

            if (TargetMind?.OwnedEntity is {Valid: true} owned)
                targetName = EntityManager.GetComponent<MetaDataComponent>(owned).EntityName;

            return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName), ("job", jobName));
        }
    }

    public string Description => Loc.GetString("objective-condition-kill-person-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Objects/Weapons/Guns/Pistols/viper.rsi"), "icon");

    public float Progress
    {
        get
        {
            var entityManager = IoCManager.Resolve<EntityManager>();
            var mindSystem = entityManager.System<MindSystem>();
            return TargetMindId == null || TargetMind == null || mindSystem.IsCharacterDeadIc(TargetMind!) ? 1f : 0f;
        }
    }

    public float Difficulty => 2f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is KillCultistTarget kpc && Equals(TargetMindId, kpc.TargetMindId);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((KillCultistTarget) obj);
    }

    public override int GetHashCode()
    {
        return TargetMindId?.GetHashCode() ?? 0;
    }
}*/
