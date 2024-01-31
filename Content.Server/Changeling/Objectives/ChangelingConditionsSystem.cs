using System.Linq;
using Content.Server.Changeling.Objectives.Components;
using Content.Server.Forensics;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared.Changeling;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Changeling.Objectives;

public sealed class ChangelingConditionsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Absorb DNA condition
        SubscribeLocalEvent<AbsorbDnaConditionComponent, ObjectiveAssignedEvent>(OnAbsorbDnaAssigned);
        SubscribeLocalEvent<AbsorbDnaConditionComponent, ObjectiveAfterAssignEvent>(OnAbsorbDnaAfterAssigned);
        SubscribeLocalEvent<AbsorbDnaConditionComponent, ObjectiveGetProgressEvent>(OnAbsorbDnaGetProgress);


        //Absorb more genomes, than others changelings
        SubscribeLocalEvent<AbsorbMoreConditionComponent, ObjectiveGetProgressEvent>(OnAbsorbMoreGetProgress);

        //Absorb other changeling
        SubscribeLocalEvent<PickRandomChangelingComponent, ObjectiveAssignedEvent>(OnAbsorbChangelingAssigned);
        SubscribeLocalEvent<AbsorbChangelingConditionComponent, ObjectiveGetProgressEvent>(OnAbsorbChangelingGetProgress);

        //Escape with identity
        SubscribeLocalEvent<PickRandomIdentityComponent, ObjectiveAssignedEvent>(OnEscapeWithIdentityAssigned);
        SubscribeLocalEvent<EscapeWithIdentityConditionComponent, ObjectiveGetProgressEvent>(OnEscapeWithIdentityGetProgress);
    }

    #region AbsorbDNA

    private void OnAbsorbDnaAssigned(EntityUid uid, AbsorbDnaConditionComponent component, ref ObjectiveAssignedEvent args)
    {
        component.NeedToAbsorb = _random.Next(2, 6);
    }

    private void OnAbsorbDnaAfterAssigned(EntityUid uid, AbsorbDnaConditionComponent component, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString("objective-condition-absorb-dna", ("count", component.NeedToAbsorb));

        _metaData.SetEntityName(uid, title, args.Meta);
    }

    private void OnAbsorbDnaGetProgress(EntityUid uid, AbsorbDnaConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetAbsorbProgress(args.Mind, component.NeedToAbsorb);
    }

    private float GetAbsorbProgress(MindComponent mind, int requiredDna)
    {
        if (!TryComp<ChangelingComponent>(mind.CurrentEntity, out var changelingComponent))
            return 0f;

        var absorbed = changelingComponent.AbsorbedEntities.Count - 1; // Because first - it's the owner

        if (requiredDna == absorbed)
            return 1f;

        var progress = MathF.Min(absorbed/(float)requiredDna, 1f);

        return progress;
    }

    #endregion

    #region AbsorbMoreDNA

    private void OnAbsorbMoreGetProgress(EntityUid uid, AbsorbMoreConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetAbsorbMoreProgress(args.Mind);
    }

    private float GetAbsorbMoreProgress(MindComponent mind)
    {
        if (!TryComp<ChangelingComponent>(mind.CurrentEntity, out var changelingComponent))
            return 0f;

        var selfAbsorbed = changelingComponent.AbsorbedEntities.Count - 1; // Because first - it's the owner

        var query = EntityQueryEnumerator<ChangelingComponent>();

        List<int> otherAbsorbed = new();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (uid == mind.CurrentEntity)
                continue; //don't include self

            var absorbed = comp.AbsorbedEntities.Count - 1;
            otherAbsorbed.Add(absorbed);
        }

        if (otherAbsorbed.Count == 0)
            return 1f;

        var isTheMost = otherAbsorbed.Max() < selfAbsorbed;

        return isTheMost ? 1f : 0f;
    }

    #endregion

    #region AbsorbChangeling

    private void OnAbsorbChangelingAssigned(EntityUid uid, PickRandomChangelingComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        if (target.Target != null)
            return;

        foreach (var changelingRule in EntityQuery<ChangelingRuleComponent>())
        {
            var changelingMinds = changelingRule.ChangelingMinds
                .Except(new List<EntityUid> { args.MindId })
                .ToList();

            if (changelingMinds.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            _target.SetTarget(uid, _random.Pick(changelingMinds), target);
        }
    }

    private void OnAbsorbChangelingGetProgress(EntityUid uid, AbsorbChangelingConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetAbsorbChangelingProgress(args.Mind, target.Value);
    }

    private float GetAbsorbChangelingProgress(MindComponent mind, EntityUid target)
    {
        if(!_mind.TryGetMind(mind.CurrentEntity!.Value, out var selfMindId, out _))
            return 0f;

        if (!TryComp<MindComponent>(target, out var targetMind))
            return 0f;

        if (!HasComp<ChangelingComponent>(targetMind.CurrentEntity))
            return 0f;

        if (!TryComp<AbsorbedComponent>(targetMind.CurrentEntity, out var absorbedComponent))
            return 0f;

        return absorbedComponent.AbsorberMind == selfMindId ? 1f : 0f;
    }

    #endregion

    #region EscapeWithIdentity

    private void OnEscapeWithIdentityAssigned(EntityUid uid, PickRandomIdentityComponent component, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        if (target.Target != null)
            return;

        var allHumans = _mind.GetAliveHumansExcept(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(uid, _random.Pick(allHumans), target);
    }

    private void OnEscapeWithIdentityGetProgress(EntityUid uid, EscapeWithIdentityConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetEscapeWithIdentityProgress(args.Mind, target.Value);
    }

    private float GetEscapeWithIdentityProgress(MindComponent mind, EntityUid target)
    {
        var progress = 0f;

        if (!TryComp<DnaComponent>(mind.CurrentEntity, out var selfDna))
            return 0f;

        if (!TryComp<MindComponent>(target, out var targetMind))
            return 0f;

        if (!TryComp<DnaComponent>(targetMind.CurrentEntity, out var targetDna))
            return 0f;

        if (!TryComp<ChangelingComponent>(mind.CurrentEntity, out var changeling))
            return 0f;

        if (!changeling.AbsorbedEntities.ContainsKey(targetDna.DNA))
            return 0f;

        //Target absorbed by this changeling, so 50% of work is done
        progress += 0.5f;

        if (_emergencyShuttle.IsTargetEscaping(mind.CurrentEntity.Value) && selfDna.DNA == targetDna.DNA)
            progress += 0.5f;

        if (_emergencyShuttle.ShuttlesLeft)
            return progress;

        return progress;
    }

    #endregion
}
