using System.Diagnostics;
using System.Linq;
using Content.Server.Objectives.Components;
using Content.Server._White.Cult.GameRule;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class KillCultistTargetConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillCultistTargetConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<KillCultistTargetConditionComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
    }

    private void OnGetProgress(EntityUid uid, KillCultistTargetConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value);
    }

    private void OnPersonAssigned(EntityUid uid, KillCultistTargetConditionComponent component, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        var cultistRule = EntityManager.EntityQuery<CultRuleComponent>().FirstOrDefault();
        Debug.Assert(cultistRule != null, nameof(cultistRule) + " != null");
        var cultTarget = cultistRule.CultTarget;

        if (cultTarget != null)
            _target.SetTarget(uid, cultTarget.Value, target);
    }

    private float GetProgress(EntityUid target)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        // dead is success
        return _mind.IsCharacterDeadIc(mind) ? 1f : 0f;
    }
}
