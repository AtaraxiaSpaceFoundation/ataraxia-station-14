using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Robust.Shared.Serialization;

namespace Content.Shared.Borer;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedBorerSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BorerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BorerComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<InfestedBorerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InfestedBorerComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<InfestedBorerComponent, ExamineAttemptEvent>(OnExamineAttempt);
        SubscribeLocalEvent<BorerComponent, ExamineAttemptEvent>(OnExamineAttempt);

        SubscribeLocalEvent<InfestedBorerComponent, BorerBrainResistEvent>(OnResistControl);

    }

    private void OnRemove(EntityUid uid, InfestedBorerComponent component, ComponentRemove args)
    {
        if (!TryComp(uid, out ActionsComponent? borerActComponent))
            return;
        _action.RemoveAction(uid, component.ActionBorerOutEntity, borerActComponent);
        _action.RemoveAction(uid, component.ActionBorerScanEntity, borerActComponent);
        _action.RemoveAction(uid, component.ActionBorerBrainTakeEntity, borerActComponent);
        _action.RemoveAction(uid, component.ActionBorerBrainSpeechEntity, borerActComponent);
        _action.RemoveAction(uid, component.ActionBorerInjectWindowOpenEntity, borerActComponent);
    }

    private void OnRemove(EntityUid uid, BorerComponent component, ComponentRemove args)
    {
        if (!TryComp(uid, out ActionsComponent? borerActComponent))
            return;
        _action.RemoveAction(uid, component.ActionInfestEntity, borerActComponent);
        _action.RemoveAction(uid, component.ActionStunEntity, borerActComponent);
    }

    private void OnResistControl(EntityUid uid, InfestedBorerComponent component, BorerBrainResistEvent args)
    {
        args.Handled = true;
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            TimeSpan.FromSeconds(30),
            new BorerBrainResistAfterEvent(), uid)
        {
            Hidden = true
        });
    }

    private void OnStartup(EntityUid uid, BorerComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;
        _action.AddAction(uid, ref component.ActionInfestEntity, component.ActionInfest, component: comp);
        _action.AddAction(uid, ref component.ActionStunEntity, component.ActionStun, component: comp);
        _metaData.SetEntityName(uid, Loc.GetString("borer-entity-name"));
        _metaData.SetEntityDescription(uid, Loc.GetString("borer-entity-description"));
        RaiseNetworkEvent(new BorerOverlayResponceEvent());
    }

    private void OnStartup(EntityUid uid, InfestedBorerComponent component, ComponentStartup args)
    {
        AddInfestedAbilities(uid, component);
    }
    private void OnExamineAttempt(EntityUid uid, InfestedBorerComponent component, ExamineAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnExamineAttempt(EntityUid uid, BorerComponent component, ExamineAttemptEvent args)
    {
        args.Cancel();
    }

    public void RaiseInjectEvent(string protoId, int cost)
    {
        RaiseNetworkEvent(new BorerInjectActionEvent(protoId, cost));
    }

    public Dictionary<string, int> GetReagents(EntityUid borerUid)
    {
        if (TryComp(borerUid, out InfestedBorerComponent? infestedComp))
            return infestedComp.AvailableReagents;
        else
            return new();
    }

    public bool AddInfestedAbilities(EntityUid uid, InfestedBorerComponent component)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return false;
        _action.AddAction(uid, ref component.ActionBorerOutEntity, component.ActionBorerOut, component: comp);
        _action.AddAction(uid, ref component.ActionBorerBrainSpeechEntity, component.ActionBorerBrainSpeech, component: comp);
        _action.AddAction(uid, ref component.ActionBorerInjectWindowOpenEntity, component.ActionBorerInjectWindowOpen, component: comp);
        _action.AddAction(uid, ref component.ActionBorerScanEntity, component.ActionBorerScan, component: comp);
        _action.AddAction(uid, ref component.ActionBorerBrainTakeEntity, component.ActionBorerBrainTake, component: comp);
        if (component.ActionBorerBrainTakeEntity.HasValue)
        {
            _metaData.SetEntityName(component.ActionBorerBrainTakeEntity.Value,
                $"{Loc.GetString("borer-abilities-control-name")} ([color=red]{component.AssumeControlCost}c[/color])");
        }

        return true;
    }

    public int GetPoints(EntityUid borerUid)
    {
        if (TryComp(borerUid, out InfestedBorerComponent? infestedComp))
            return infestedComp.Points;
        else return 0;

    }

    public EntityUid? GetHost(EntityUid borerUid)
    {
        if (TryComp(borerUid, out InfestedBorerComponent? infestedComp))
            return infestedComp.Host;
        else
            return null;

    }
}

public sealed partial class BorerInfestActionEvent : EntityTargetActionEvent {}

public sealed partial class BorerOutActionEvent : InstantActionEvent {}

public sealed partial class BorerBrainSpeechActionEvent : InstantActionEvent {}

public sealed partial class BorerInjectWindowOpenEvent : InstantActionEvent{}

public sealed partial class BorerBrainTakeEvent : InstantActionEvent{}

public sealed partial class BorerBrainReleaseEvent : InstantActionEvent{}

public sealed partial class BorerBrainResistEvent : InstantActionEvent{}

public sealed partial class BorerStunActionEvent : EntityTargetActionEvent{}

public sealed partial class BorerReproduceEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public sealed partial class BorerPointsUpdateEvent : EntityEventArgs{}

[Serializable, NetSerializable]
public sealed partial class BorerInjectActionEvent : EntityEventArgs
{
    public string ProtoId;

    public int Cost;

    public BorerInjectActionEvent(string protoId, int cost)
    {
        ProtoId = protoId;
        Cost = cost;
    }
}
public sealed partial class BorerScanInstantActionEvent : InstantActionEvent
{
}
