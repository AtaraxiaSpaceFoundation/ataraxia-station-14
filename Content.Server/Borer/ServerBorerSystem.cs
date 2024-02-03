using Content.Server.Administration;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Medical;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Actions;
using Content.Shared.Borer;
using Content.Shared.Changeling;
using Content.Shared.Chat;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Borer;

public sealed class ServerBorerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    [Dependency] private readonly StunSystem _stuns = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    [Dependency] private readonly SharedBorerSystem _sharedBorerSystem = default!;

    [Dependency] private readonly MetaDataSystem _metaData = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorerComponent, BorerInfestActionEvent>(OnInfest);
        SubscribeLocalEvent<BorerComponent, BorerInfestDoAfterEvent>(OnInfestAfter);

        SubscribeLocalEvent<InfestedBorerComponent, BorerOutActionEvent>(OnGetOut);
        SubscribeLocalEvent<InfestedBorerComponent, BorerBrainSpeechActionEvent>(OnTelepathicSpeech);

        SubscribeNetworkEvent<BorerInjectActionEvent>(OnInjectChemicals);

        SubscribeLocalEvent<InfestedBorerComponent, BorerScanInstantActionEvent>(OnBorerScan);
        SubscribeLocalEvent<BorerComponent, BorerStunActionEvent>(OnStunEvent);

        SubscribeLocalEvent<InfestedBorerComponent, BorerBrainTakeEvent>(OnTakeControl);
        SubscribeLocalEvent<InfestedBorerComponent, BorerBrainTakeAfterEvent>(OnTakeControlAfter);

        SubscribeLocalEvent<BorerHostComponent, BorerBrainReleaseEvent>(OnReleaseControl);

        SubscribeLocalEvent<BorerHostComponent, BorerReproduceEvent>(OnReproduce);
        SubscribeLocalEvent<BorerHostComponent, BorerReproduceAfterEvent>(OnReproduceAfter);
        SubscribeLocalEvent<InfestedBorerComponent, BorerBrainResistAfterEvent>(OnResistAfterControl);
    }

    private void OnReproduceAfter(EntityUid uid, BorerHostComponent component, BorerReproduceAfterEvent args)
    {
        if (args.Cancelled || !TryComp(component.BorerContainer.ContainedEntities[0], out InfestedBorerComponent? borerComp)
                           || !WithrawPoints(component.BorerContainer.ContainedEntities[0], borerComp.ReproduceCost)
                           || !TryComp(uid, out TransformComponent? targetTransform))
            return;
        args.Handled = true;
        _vomitSystem.Vomit(uid, -30, -30);
        _entityManager.SpawnEntity("MobSimpleBorer", targetTransform.Coordinates);
    }

    private void OnReproduce(EntityUid uid, BorerHostComponent component, BorerReproduceEvent args)
    {
        if (!TryComp(component.BorerContainer.ContainedEntities[0], out InfestedBorerComponent? borerComp))
            return;
        if (GetPoints(component.BorerContainer.ContainedEntities[0]) < borerComp.ReproduceCost)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-lowchem"),
                uid,
                uid, PopupType.LargeCaution);
            return;
        }

        args.Handled = true;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            TimeSpan.FromSeconds(3),
            new BorerReproduceAfterEvent(), uid)
        {
            Hidden = true
        });
    }

    public void ReleaseControl(EntityUid borerUid)
    {
        if (!TryComp(borerUid, out ActionsComponent? wormActComp) ||
            !TryComp(borerUid, out InfestedBorerComponent? borComp) ||
            !borComp.Host.HasValue ||
            !borComp.ControllingBrain)
            return;

        TryComp(borComp.Host.Value, out ActionsComponent? bodyActComp);
        var wormHasMind = _mindSystem.TryGetMind(borerUid, out var hostMindId, out var hostMind);
        var bodyHasMind = _mindSystem.TryGetMind(borComp.Host.Value, out var mindId, out var mind);
        if (!bodyHasMind && !wormHasMind)
            return;

        if (wormHasMind)
            _mindSystem.TransferTo(hostMindId, borComp.Host.Value, mind: hostMind);
        if (bodyHasMind)
            _mindSystem.TransferTo(mindId, borerUid, mind: mind);


        _sharedBorerSystem.AddInfestedAbilities(borerUid, borComp);

        _action.RemoveAction(borerUid, borComp.ActionBorerBrainResistEntity, wormActComp);

        _action.RemoveAction(borComp.Host.Value, borComp.ActionBorerBrainReleaseEntity, bodyActComp);

        _action.RemoveAction(borComp.Host.Value, borComp.ActionBorerReproduceEntity, bodyActComp);

        borComp.ControllingBrain = false;
        Dirty(borerUid, borComp);
    }

    private void OnReleaseControl(EntityUid uid, BorerHostComponent component, BorerBrainReleaseEvent args)
    {
        args.Handled = true;
        ReleaseControl(component.BorerContainer.ContainedEntities[0]);
    }

    private void OnResistAfterControl(EntityUid uid, InfestedBorerComponent component, BorerBrainResistAfterEvent args)
    {
        if (args.Cancelled)
            return;
        ReleaseControl(uid);
    }

    private void OnTakeControl(EntityUid uid, InfestedBorerComponent component, BorerBrainTakeEvent args)
    {
        if (GetPoints(uid) < component.AssumeControlCost)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-lowchem"),
                uid,
                uid, PopupType.LargeCaution);
            return;
        }

        if (GetSugarQuantityInHost(uid) > 0)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-toomuchsugar"), uid,
                uid, PopupType.LargeCaution);
        }

        if (TryComp(component.Host, out MobStateComponent? state) &&
            state.CurrentState == MobState.Critical)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-braintake-critical"), uid,
                uid, PopupType.LargeCaution);
            return;
        }

        args.Handled = true;
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            TimeSpan.FromSeconds(30),
            new BorerBrainTakeAfterEvent(), uid)
        {
            Hidden = true
        });
    }

    private void OnTakeControlAfter(EntityUid uid, InfestedBorerComponent component, BorerBrainTakeAfterEvent args)
    {
        if (!TryComp(uid, out ActionsComponent? comp) ||
            !TryComp(component.Host, out ActionsComponent? hostComp))
            return;

        else if (TryComp(component.Host, out MobStateComponent? state) &&
            state.CurrentState == MobState.Critical)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-braintake-critical"), uid,
                uid, PopupType.LargeCaution);
            return;
        }
        else if (GetSugarQuantityInHost(uid) > 0)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-toomuchsugar"), uid,
                uid, PopupType.LargeCaution);
        }
        else if (args.Cancelled || !WithrawPoints(uid, component.AssumeControlCost))
            return;

        var borHasMind = _mindSystem.TryGetMind(uid, out var mindId, out var mind);
        var hostHasMind = _mindSystem.TryGetMind(component.Host.Value, out var hostMindId, out var hostMind);

        if (!borHasMind && !hostHasMind)
            return;
        if (borHasMind)
        {
            _mindSystem.TransferTo(mindId, component.Host, mind: mind);
            _popup.PopupEntity(Loc.GetString("borer-popup-braintake-success"), component.Host.Value,
                component.Host.Value, PopupType.Large);


        }

        if (hostHasMind)
        {
            _mindSystem.TransferTo(hostMindId, uid, mind: hostMind);
            _popup.PopupEntity(Loc.GetString("borer-popup-braintake-alert"), uid, uid, PopupType.LargeCaution);
            if (EntityManager.TryGetComponent(uid, out ActorComponent? actor))
            {
                _chatManager.ChatMessageToOne(ChatChannel.Local,
                    Loc.GetString("borer-message-braintake-alert"),
                    Loc.GetString("borer-message-braintake-alert"),
                    EntityUid.Invalid, false, actor.PlayerSession.Channel);
            }
        }

        _action.RemoveAction(uid, component.ActionBorerOutEntity, comp);
        _action.RemoveAction(uid, component.ActionBorerScanEntity, comp);
        _action.RemoveAction(uid, component.ActionBorerBrainTakeEntity, comp);
        _action.RemoveAction(uid, component.ActionBorerInjectWindowOpenEntity, comp);
        _action.RemoveAction(uid, component.ActionBorerReproduceEntity, comp);

        _action.AddAction(uid, ref component.ActionBorerBrainResistEntity,
            component.ActionBorerBrainResist, component: comp);

        _action.AddAction(component.Host.Value, ref component.ActionBorerBrainReleaseEntity,
            component.ActionBorerBrainRelease, component: hostComp);
        _action.AddAction(component.Host.Value, ref component.ActionBorerReproduceEntity,
            component.ActionBorerReproduce, component: hostComp);

        if(component.ActionBorerReproduceEntity.HasValue)
            _metaData.SetEntityName(component.ActionBorerReproduceEntity.Value,
                $"{Loc.GetString("borer-abilities-reproduce-name")} ([color=red]{component.ReproduceCost}c[/color])");

        component.ControllingBrain = true;
        Dirty(uid, component);
    }

    private void OnStunEvent(EntityUid uid, BorerComponent component, BorerStunActionEvent args)
    {
        _stuns.TryParalyze(args.Target, TimeSpan.FromSeconds(5.7f), true);
        args.Handled = true;
    }

    private void OnInfest(EntityUid uid, BorerComponent component, BorerInfestActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target)
            || HasComp<ChangelingComponent>(args.Target)
            || HasComp<BorgChassisComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-infest-failed"), uid, uid);
            return;
        }

        if (TryComp(args.Target, out MobStateComponent? state) &&
            state.CurrentState == MobState.Dead)
            return;

        if (HasComp<BorerHostComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-infest-occupied"), uid, uid);
            args.Handled = true;
            return;
        }

        if (GetSugarQuantityInEntity(args.Target) > 10)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-infest-sugar"), uid, uid, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        StartInfest(uid, args.Target, component);
        args.Handled = true;
    }

    private void OnInfestAfter(EntityUid uid, BorerComponent component, BorerInfestDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;
        if (TryComp(args.Target, out MobStateComponent? state) &&
            state.CurrentState == MobState.Dead)
            return;

        if (HasComp<BorerHostComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-infest-occupied"), uid, uid);
            args.Handled = true;
            return;
        }

        if (GetSugarQuantityInHost(uid) > 10)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-infest-sugar"), uid, uid, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        var hostComp = AddComp<BorerHostComponent>(args.Target.Value);
        hostComp.BorerContainer = _container.EnsureContainer<Container>(args.Target.Value, "borerContainer");
        _container.Insert(uid, hostComp.BorerContainer);

        var infestedComponent = AddComp<InfestedBorerComponent>(uid);
        infestedComponent.Host = args.Target;
        infestedComponent.Points = component.Points;
        Dirty(uid, infestedComponent);

        RemComp<BorerComponent>(uid);
    }

    private void StartInfest(EntityUid user, EntityUid target, BorerComponent comp)
    {
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            TimeSpan.FromSeconds(5),
            new BorerInfestDoAfterEvent(), user, target)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            Hidden = true
        });
    }

    private void OnBorerScan(EntityUid uid, InfestedBorerComponent component, BorerScanInstantActionEvent args)
    {
        if (!component.Host.HasValue)
            return;
        Dictionary<string, FixedPoint2> solution = new();
        if (EntityManager.TryGetComponent(component.Host.Value,
                out BloodstreamComponent? bloodContainer))
        {
            if(_solutionContainerSystem.TryGetSolution(component.Host.Value, bloodContainer.ChemicalSolutionName,
                out var sol))
            {
                foreach (var reagent in sol.Value.Comp.Solution)
                {
                    solution.Add(reagent.Reagent.ToString(), reagent.Quantity);
                }
            }
        }

        RaiseNetworkEvent(new BorerScanDoAfterEvent(solution), uid);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var infestedQuery = EntityQueryEnumerator<InfestedBorerComponent>();
        while (infestedQuery.MoveNext(out var uid, out var comp) && comp.Host is not null)
        {
            if (comp.PointUpdateNext == TimeSpan.Zero)
            {
                comp.PointUpdateNext = _timing.CurTime + comp.PointUpdateRate;
                continue;
            }

            if (_timing.CurTime < comp.PointUpdateNext)
                continue;

            if (GetSugarQuantityInHost(uid) > 30)
            {
                GetOut(uid);
                _popup.PopupEntity(Loc.GetString("borer-popup-sugarleave"), uid, uid, PopupType.LargeCaution);
                return;
            }

            comp.PointUpdateNext += comp.PointUpdateRate;
            AddPoints(uid, comp.PointUpdateValue);
            Dirty(uid, comp);
        }
    }

    private void OnTelepathicSpeech(EntityUid uid, InfestedBorerComponent component, BorerBrainSpeechActionEvent args)
    {
        if (!EntityManager.TryGetComponent(uid, out ActorComponent? actor))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("borer-ui-converse-title"),
            Loc.GetString("borer-ui-converse-message"), (string message) =>
            {
                _popup.PopupEntity(message, uid, uid, PopupType.Medium);

                if (EntityManager.TryGetComponent(component.Host, out ActorComponent? hostActor))
                {
                    _popup.PopupEntity(message, component.Host.Value, component.Host.Value,
                        PopupType.Medium);
                }
            });
        args.Handled = true;
    }

    private void OnInjectChemicals(BorerInjectActionEvent injectEvent, EntitySessionEventArgs eventArgs)
    {
        var borerEn = eventArgs.SenderSession.AttachedEntity;
        if (EntityManager.TryGetComponent(borerEn,
                out InfestedBorerComponent? infestedComponent) &&
            infestedComponent.Host.HasValue)
        {
            if (!WithrawPoints(borerEn.Value, injectEvent.Cost))
                return;

            var solution = new Solution();
            solution.AddReagent(injectEvent.ProtoId, 10);
            _bloodstreamSystem.TryAddToChemicals(infestedComponent.Host.Value, solution);
            _reactiveSystem.DoEntityReaction(infestedComponent.Host.Value, solution, ReactionMethod.Injection);

            _popup.PopupEntity(Loc.GetString("borer-popup-injected", ("reagent", Loc.GetString("reagent-name-"+
                    injectEvent.ProtoId.ToLower().Replace("spacedrugs", "space-drugs")))),
                borerEn.Value, borerEn.Value, PopupType.Medium);
        }
    }

    public bool AddPoints(EntityUid borerUid, int value)
    {
        if (!EntityManager.TryGetComponent(borerUid,
                out InfestedBorerComponent? infestedComponent))
            return false;

        infestedComponent.Points += value;
        Dirty(borerUid, infestedComponent);
        RaiseNetworkEvent(new BorerPointsUpdateEvent());
        return true;
    }

    public int GetPoints(EntityUid borerUid)
    {
        if (!EntityManager.TryGetComponent(borerUid,
                out InfestedBorerComponent? infestedComponent))
            return 0;

        return infestedComponent.Points;
    }

    public bool WithrawPoints(EntityUid borerUid, int value)
    {
        if (!EntityManager.TryGetComponent(borerUid,
                out InfestedBorerComponent? infestedComponent) || infestedComponent.Points < value)
        {
            _popup.PopupEntity(Loc.GetString("borer-popup-lowchem"),
                borerUid,
                borerUid, PopupType.LargeCaution);
            return false;
        }

        infestedComponent.Points -= value;
        Dirty(borerUid, infestedComponent);
        RaiseNetworkEvent(new BorerPointsUpdateEvent());
        return true;
    }

    private void OnGetOut(EntityUid uid, InfestedBorerComponent component, BorerOutActionEvent args)
    {
        GetOut(uid);
    }

    public void GetOut(EntityUid uid)
    {
        if (!TryComp(uid, out InfestedBorerComponent? component) ||
            !TryComp(component.Host, out BorerHostComponent? hostComponent))
            return;
        ReleaseControl(uid);

        _vomitSystem.Vomit(component.Host.Value, -20, -20);
        _container.Remove(uid, hostComponent.BorerContainer);
        RemComp<BorerHostComponent>(component.Host.Value);

        var borerComponent = AddComp<BorerComponent>(uid);
        borerComponent.Points = component.Points;
        Dirty(uid, borerComponent);

        RemComp<InfestedBorerComponent>(uid);
        _action.SetCooldown(borerComponent.ActionStunEntity, _timing.CurTime, _timing.CurTime+TimeSpan.FromSeconds(20));
        _action.SetCooldown(borerComponent.ActionInfestEntity, _timing.CurTime, _timing.CurTime+TimeSpan.FromSeconds(20));
    }

    public int GetSugarQuantityInHost(EntityUid borerUid)
    {
        var sugarQuantity = 0;
        if (EntityManager.TryGetComponent(borerUid,
                out InfestedBorerComponent? component) &&
            component.Host.HasValue)
        {
            sugarQuantity = GetSugarQuantityInEntity(component.Host.Value);
        }

        return sugarQuantity;
    }

    public int GetSugarQuantityInEntity(EntityUid uid)
    {
        if (EntityManager.TryGetComponent(uid,
                out BloodstreamComponent? bloodContainer))
        {
            if(_solutionContainerSystem.TryGetSolution(uid, bloodContainer.ChemicalSolutionName,
                out var sol))
            {
                foreach (var reagent in sol.Value.Comp.Solution)
                {
                    if (reagent.Reagent.ToString() == "Sugar")
                    {
                        return reagent.Quantity.Int();
                    }
                }
            }
        }
        return 0;
    }
}
