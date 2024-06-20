using System.Linq;
using Content.Server._Miracle.GulagSystem;
using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Bible.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared._White;
using Content.Shared._White.Cult.Components;
using Content.Shared._White.Cult.Systems;
using Content.Shared._White.Mood;
using Content.Shared.Cloning;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Robust.Server.Containers;
using Robust.Server.Player;

namespace Content.Server._White.Cult.GameRule;

public sealed class CultRuleSystem : GameRuleSystem<CultRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly GulagSystem _gulag = default!;
    [Dependency] private readonly BloodSpearSystem _bloodSpear = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    private const int PlayerPerCultist = 10;
    private int _minStartingCultists;
    private int _maxStartingCultists;

    public override void Initialize()
    {
        base.Initialize();

        _minStartingCultists = _cfg.GetCVar(WhiteCVars.CultMinStartingPlayers);
        _maxStartingCultists = _cfg.GetCVar(WhiteCVars.CultMaxStartingPlayers);

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<CultNarsieSummoned>(OnNarsieSummon);

        SubscribeLocalEvent<CultistComponent, ComponentInit>(OnCultistComponentInit);
        SubscribeLocalEvent<CultistComponent, ComponentRemove>(OnCultistComponentRemoved);
        SubscribeLocalEvent<CultistComponent, MobStateChangedEvent>(OnCultistsStateChanged);
        SubscribeLocalEvent<CultistComponent, CloningEvent>(OnClone);

        SubscribeLocalEvent<CultistRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnClone(Entity<CultistComponent> ent, ref CloningEvent args)
    {
        RemoveObjectiveAndRole(ent);
    }

    protected override void Added(EntityUid uid, CultRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        gameRule.MinPlayers = _cfg.GetCVar(WhiteCVars.CultMinPlayers);
    }

    private void OnGetBriefing(Entity<CultistRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("cult-role-briefing-short"));
        args.Append(Loc.GetString("cult-role-briefing-hint"));
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, "CULT");
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            DoCultistsStart(cult);
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            var winText = Loc.GetString($"cult-condition-{cult.WinCondition.ToString().ToLower()}");
            ev.AddLine(winText);

            ev.AddLine(Loc.GetString("cultists-list-start"));

            foreach (var (entityName, ckey) in cult.CultistsCache)
            {
                var lising = Loc.GetString("cultists-list-name", ("name", entityName), ("user", ckey));
                ev.AddLine(lising);
            }
        }
    }

    private void OnNarsieSummon(CultNarsieSummoned ev)
    {
        var query =
            EntityQueryEnumerator<MobStateComponent, MindContainerComponent, CultistComponent, TransformComponent>();

        List<Entity<MindContainerComponent, TransformComponent>> cultists = new();

        while (query.MoveNext(out var uid, out _, out var mindContainer, out _, out var transform))
        {
            cultists.Add((uid, mindContainer, transform));
        }

        var rulesQuery = QueryActiveRules();
        while (rulesQuery.MoveNext(out _, out var cult, out _))
        {
            cult.WinCondition = CultWinCondition.Win;
            _roundEndSystem.EndRound();

            foreach (var ent in cultists)
            {
                if (ent.Comp1.Mind is null)
                    continue;

                var reaper = Spawn(cult.ReaperPrototype, ent.Comp2.Coordinates);
                _mindSystem.TransferTo(ent.Comp1.Mind.Value, reaper);

                _bodySystem.GibBody(ent);
            }

            return;
        }
    }

    private void OnCultistComponentInit(EntityUid uid, CultistComponent component, ComponentInit args)
    {
        RaiseLocalEvent(uid, new MoodEffectEvent("CultFocused"));

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            cult.CurrentCultists.Add(component);

            var name = Name(uid);

            if (TryComp<ActorComponent>(uid, out var actor))
            {
                cult.CultistsCache.TryAdd(name, actor.PlayerSession.Name);
            }

            UpdateCultistsAppearance(cult);
        }
    }

    public void RemoveObjectiveAndRole(EntityUid uid)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var objectives = mind.Objectives.FindAll(HasComp<PickCultTargetComponent>);
        foreach (var obj in objectives)
        {
            _mindSystem.TryRemoveObjective(mindId, mind, mind.Objectives.IndexOf(obj));
        }

        if (_roleSystem.MindHasRole<CultistRoleComponent>(mindId))
            _roleSystem.MindRemoveRole<CultistRoleComponent>(mindId);
    }

    private void OnCultistComponentRemoved(EntityUid uid, CultistComponent component, ComponentRemove args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            cult.CurrentCultists.Remove(component);
        }

        if (!TerminatingOrDeleted(uid))
        {
            RemoveAllCultistItems(uid);
            RemoveCultistAppearance(uid);
            RaiseLocalEvent(uid, new MoodRemoveEffectEvent("CultFocused"));
        }

        _bloodSpear.DetachSpearFromUser((uid, component));

        foreach (var empower in component.SelectedEmpowers)
        {
            _actions.RemoveAction(uid, GetEntity(empower));
        }

        CheckRoundShouldEnd();
    }

    private void OnCultistsStateChanged(EntityUid uid, CultistComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
        {
            CheckRoundShouldEnd();
        }
    }

    private void DoCultistsStart(CultRuleComponent rule)
    {
        var eligiblePlayers = _antagSelection.GetEligiblePlayers(_playerManager.Sessions, rule.CultistRolePrototype,
            customExcludeCondition: HasComp<BibleUserComponent>);

        if (eligiblePlayers.Count == 0)
        {
            return;
        }

        var cultistsToSelect =
            Math.Clamp(_playerManager.PlayerCount / PlayerPerCultist, _minStartingCultists, _maxStartingCultists);

        var selectedCultists = _antagSelection.ChooseAntags(cultistsToSelect, eligiblePlayers);

        var potentialTargets = FindPotentialTargets(selectedCultists);
        rule.CultTarget = _random.PickAndTake(potentialTargets).Mind;

        foreach (var cultist in selectedCultists)
        {
            MakeCultist(cultist, rule);
        }
    }

    public MindComponent? GetTarget()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            if (cult.CultTarget == null || !TryComp(cult.CultTarget.Value, out MindComponent? mind))
            {
                continue;
            }

            return mind;
        }

        return null;
    }

    public bool CanSummonNarsie()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            var cultistsAmount = cult.CurrentCultists.Count;
            var constructsAmount = cult.Constructs.Count;
            var enoughCultists = cultistsAmount + constructsAmount > 10;

            if (!enoughCultists)
            {
                return false;
            }

            var target = GetTarget();
            var targetKilled = target == null || _mindSystem.IsCharacterDeadIc(target);

            return targetKilled;
        }

        return false;
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            var aliveCultists = 0;

            foreach (var cultistComponent in cult.CurrentCultists)
            {
                var owner = cultistComponent.Owner;
                if (!TryComp<MobStateComponent>(owner, out var mobState))
                    continue;

            if (!_mobStateSystem.IsDead(owner, mobState))
            {
                aliveCultists++;
            }
        }

            if (aliveCultists != 0)
                return;

            cult.WinCondition = CultWinCondition.Failure;

            // Check for all at once gamemode
            if (!GameTicker.GetActiveGameRules().Where(HasComp<RampingStationEventSchedulerComponent>).Any())
                _roundEndSystem.EndRound();
        }
    }

    private void RemoveCultistAppearance(EntityUid cultist)
    {
        if (TryComp<HumanoidAppearanceComponent>(cultist, out var appearanceComponent))
        {
            //Потому что я так сказал
            appearanceComponent.EyeColor = Color.White;
            Dirty(cultist, appearanceComponent);
        }

        RemComp<PentagramComponent>(cultist);
    }

    private void UpdateCultistsAppearance(CultRuleComponent cultRuleComponent)
    {
        var cultistsCount = cultRuleComponent.CurrentCultists.Count;
        var constructsCount = cultRuleComponent.Constructs.Count;
        var totalCultMembers = cultistsCount + constructsCount;
        if (totalCultMembers < cultRuleComponent.ReadEyeThreshold)
            return;

        foreach (var cultistComponent in cultRuleComponent.CurrentCultists)
        {
            if (TryComp<HumanoidAppearanceComponent>(cultistComponent.Owner, out var appearanceComponent))
            {
                appearanceComponent.EyeColor = cultRuleComponent.EyeColor;
                Dirty(cultistComponent.Owner, appearanceComponent);
            }

            if (totalCultMembers < cultRuleComponent.PentagramThreshold)
                return;

            EnsureComp<PentagramComponent>(cultistComponent.Owner);
        }
    }

    private List<MindContainerComponent> FindPotentialTargets(List<EntityUid> exclude = null!)
    {
        var querry =
            EntityManager.EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent, ActorComponent>();

        var potentialTargets = new List<MindContainerComponent>();

        while (querry.MoveNext(out var uid, out var mind, out _, out var actor))
        {
            var entity = mind.Mind;

            if (entity == default)
                continue;

            if (_gulag.IsUserGulagged(actor.PlayerSession.UserId, out _))
                continue;

            if (exclude?.Contains(uid) is true)
            {
                continue;
            }

            potentialTargets.Add(mind);
        }

        return potentialTargets;
    }

    public void AdminMakeCultist(EntityUid entity)
    {
        var cultistRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistRule == null)
        {
            GameTicker.StartGameRule("Cult", out var ruleEntity);
            cultistRule = Comp<CultRuleComponent>(ruleEntity);
        }

        if (HasComp<CultistComponent>(entity))
            return;

        MakeCultist(entity, cultistRule);
    }

    public bool MakeCultist(EntityUid cultist, CultRuleComponent rule)
    {
        if (!_mindSystem.TryGetMind(cultist, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked cultist.");
            return false;
        }

        if (HasComp<CultistRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is already a cultist.");
            return false;
        }

        var briefing = Loc.GetString("cult-role-greeting");
        _antagSelection.SendBriefing(cultist, briefing, null, rule.GreetingsSound);

        _roleSystem.MindAddRole(mindId, new CultistRoleComponent
        {
            PrototypeId = rule.CultistRolePrototype
        });

        EnsureComp<CultistComponent>(cultist);

        _factionSystem.RemoveFaction(cultist, "NanoTrasen", false);
        _factionSystem.AddFaction(cultist, "Cultist");

        if (_inventorySystem.TryGetSlotEntity(cultist, "back", out var backPack))
        {
            foreach (var itemPrototype in rule.StartingItems)
            {
                var itemEntity = Spawn(itemPrototype, Transform(cultist).Coordinates);

                if (backPack != null)
                {
                    _storageSystem.Insert(backPack.Value, itemEntity, out _);
                }
            }
        }

        _mindSystem.TryAddObjective(mindId, mind, "KillCultTargetObjective");

        return true;
    }

    private void RemoveAllCultistItems(EntityUid uid)
    {
        if (!_inventorySystem.TryGetContainerSlotEnumerator(uid, out var enumerator))
            return;

        while (enumerator.MoveNext(out var container))
        {
            if (container.ContainedEntity != null && HasComp<CultItemComponent>(container.ContainedEntity.Value))
            {
                _container.Remove(container.ContainedEntity.Value, container, true, true);
            }
        }
    }

    public void TransferRole(EntityUid transferFrom, EntityUid transferTo)
    {
        if (HasComp<PentagramComponent>(transferFrom))
            EnsureComp<PentagramComponent>(transferTo);

        if (!HasComp<CultistComponent>(transferFrom))
            return;

        var query = EntityQuery<CultRuleComponent>();
        foreach (var cultRule in query)
        {
            cultRule.CultistsCache.Remove(Name(transferFrom));
        }

        EnsureComp<CultistComponent>(transferTo);
        RemComp<CultistComponent>(transferFrom);
    }
}
