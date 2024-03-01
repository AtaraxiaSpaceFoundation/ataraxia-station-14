using System.Linq;
using Content.Server._Miracle.Components;
using Content.Server._Miracle.GulagSystem;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.NPC.Systems;
using Content.Server.Roles.Jobs;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared._White;
using Content.Shared._White.Chaplain;
using Content.Shared._White.Cult.Components;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;

namespace Content.Server._White.Cult.GameRule;

public sealed class CultRuleSystem : GameRuleSystem<CultRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly JobSystem _jobSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly GulagSystem _gulag = default!;

    private ISawmill _sawmill = default!;

    private int _minimalCultists;
    private int _cultGameRuleMinimapPlayers;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("preset");
        _minimalCultists = _cfg.GetCVar(WhiteCVars.CultMinStartingPlayers);
        _cultGameRuleMinimapPlayers = _cfg.GetCVar(WhiteCVars.CultMinPlayers);

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<CultNarsieSummoned>(OnNarsieSummon);

        SubscribeLocalEvent<CultistComponent, ComponentInit>(OnCultistComponentInit);
        SubscribeLocalEvent<CultistComponent, ComponentRemove>(OnCultistComponentRemoved);
        SubscribeLocalEvent<CultistComponent, MobStateChangedEvent>(OnCultistsStateChanged);
    }

    private void OnCultistsStateChanged(EntityUid uid, CultistComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
        {
            CheckRoundShouldEnd();
        }
    }

    public MindComponent? GetTarget()
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();

        if (cultistsRule?.CultTarget == null || !TryComp<MindComponent>(cultistsRule.CultTarget.Value, out var mind))
        {
            return null;
        }

        return mind;
    }

    public bool CanSummonNarsie()
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistsRule is null)
        {
            return false;
        }

        var cultistsAmount = cultistsRule.CurrentCultists.Count;
        var constructsAmount = cultistsRule.Constructs.Count;
        var enoughCultists = cultistsAmount + constructsAmount > 10;

        if (!enoughCultists)
        {
            return false;
        }

        var target = GetTarget();
        var targetKilled = target == null || _mindSystem.IsCharacterDeadIc(target);

        return targetKilled;
    }

    private void CheckRoundShouldEnd()
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistsRule is null)
        {
            return;
        }

        var aliveCultists = 0;

        foreach (var cultistComponent in cultistsRule.CurrentCultists)
        {
            var owner = cultistComponent.Owner;
            if (!TryComp<MobStateComponent>(owner, out var mobState))
                continue;

            if (_mobStateSystem.IsAlive(owner, mobState))
            {
                aliveCultists++;
            }
        }

        if (aliveCultists != 0)
            return;

        cultistsRule.WinCondition = CultWinCondition.CultFailure;
        _roundEndSystem.EndRound();
    }

    private void OnCultistComponentInit(EntityUid uid, CultistComponent component, ComponentInit args)
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistsRule is null)
        {
            return;
        }

        if (!TryComp<MindContainerComponent>(uid, out var mindComponent))
            return;

        if (!mindComponent.HasMind)
            return;

        cultistsRule.CurrentCultists.Add(component);

        if (TryComp<ActorComponent>(uid, out var actor))
        {
            cultistsRule.CultistsCache.Add(MetaData(uid).EntityName, actor.PlayerSession.Name);
        }

        UpdateCultistsAppearance(cultistsRule);
    }

    private void OnCultistComponentRemoved(EntityUid uid, CultistComponent component, ComponentRemove args)
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistsRule is null)
        {
            return;
        }

        cultistsRule.CurrentCultists.Remove(component);

        foreach (var empower in component.SelectedEmpowers)
        {
            _actions.RemoveAction(uid, GetEntity(empower));
        }

        RemoveCultistAppearance(uid);
        CheckRoundShouldEnd();
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
        if (totalCultMembers < CultRuleComponent.ReadEyeThreshold)
            return;

        foreach (var cultistComponent in cultRuleComponent.CurrentCultists)
        {
            if (TryComp<HumanoidAppearanceComponent>(cultistComponent.Owner, out var appearanceComponent))
            {
                appearanceComponent.EyeColor = CultRuleComponent.EyeColor;
                Dirty(appearanceComponent);
            }

            if (totalCultMembers < CultRuleComponent.PentagramThreshold)
                return;

            EnsureComp<PentagramComponent>(cultistComponent.Owner);
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistsRule is null)
        {
            return;
        }

        var winText = Loc.GetString($"cult-cond-{cultistsRule.WinCondition.ToString().ToLower()}");
        ev.AddLine(winText);

        ev.AddLine(Loc.GetString("cultists-list-start"));

        foreach (var (entityName, ckey) in cultistsRule.CultistsCache)
        {
            var lising = Loc.GetString("cultists-list-name", ("name", entityName), ("user", ckey));
            ev.AddLine(lising);
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistsRule is null)
        {
            return;
        }

        var minPlayers = _cultGameRuleMinimapPlayers;
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-not-enough-ready-players",
                ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));

            ev.Cancel();
            return;
        }

        if (ev.Players.Length != 0)
            return;

        _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
        ev.Cancel();
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var cultistsRule = EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultistsRule is null)
        {
            return;
        }

        foreach (var player in ev.Players)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
                continue;

            cultistsRule.StarCandidates[player] = ev.Profiles[player.UserId];
        }

        var potentialCultists = FindPotentialCultist(cultistsRule.StarCandidates);
        var pickedCultist = PickCultists(potentialCultists);
        var potentialTargets = FindPotentialTargets(pickedCultist);

        cultistsRule.CultTarget = _random.PickAndTake(potentialTargets).Mind;

        foreach (var pickerCultist in pickedCultist)
        {
            MakeCultist(pickerCultist);
        }
    }

    private List<MindContainerComponent> FindPotentialTargets(List<ICommonSession> exclude = null!)
    {
        var querry = EntityManager.EntityQuery<MindContainerComponent, HumanoidAppearanceComponent, ActorComponent>();

        var potentialTargets = new List<MindContainerComponent>();

        foreach (var (mind, _, actor) in querry)
        {
            var entity = mind.Mind;

            if (entity == default)
                continue;

            if (_gulag.IsUserGulaged(actor.PlayerSession.UserId, out _))
                continue;

            if (exclude?.Contains(actor.PlayerSession) is true)
            {
                continue;
            }

            potentialTargets.Add(mind);
        }

        return potentialTargets;
    }

    private List<ICommonSession> FindPotentialCultist(
        in Dictionary<ICommonSession, HumanoidCharacterProfile> candidates)
    {
        var list = new List<ICommonSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            // Gulag
            if (_gulag.IsUserGulaged(player.UserId, out _))
                continue;

            // Role prevents antag.
            if (!_jobSystem.CanBeAntag(player))
                continue;

            // Chaplain
            if (!_mindSystem.TryGetMind(player, out _, out var mind) ||
                mind.OwnedEntity is not { } ownedEntity || HasComp<HolyComponent>(ownedEntity))
                continue;

            // Latejoin
            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
                continue;

            list.Add(player);
        }

        var prefList = new List<ICommonSession>();

        foreach (var player in list)
        {
            var profile = candidates[player];

            if (profile.AntagPreferences.Contains(CultRuleComponent.CultistPrototypeId))
            {
                prefList.Add(player);
            }
        }

        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient preferred cultists, picking at random.");
            prefList = list;
            return prefList;
        }

        if (prefList.Count >= _minimalCultists)
        {
            return prefList;
        }

        var playersToAdd = _minimalCultists - prefList.Count;

        foreach (var prefPlayer in prefList)
        {
            list.Remove(prefPlayer);
        }

        for (var i = 0; i < playersToAdd; i++)
        {
            var randomPlayer = _random.PickAndTake(list);
            prefList.Add(randomPlayer);
        }

        return prefList;
    }

    private List<ICommonSession> PickCultists(List<ICommonSession> prefList)
    {
        var result = new List<ICommonSession>();

        var maxCultists = _cfg.GetCVar(WhiteCVars.CultMaxStartingPlayers);

        if (prefList.Count < _minimalCultists)
        {
            _sawmill.Info("Insufficient ready players to fill up with cultists, stopping the selection.");
            return result;
        }

        var actualCultistCount = prefList.Count > maxCultists ? maxCultists : _minimalCultists;

        for (var i = 0; i < actualCultistCount; i++)
        {
            result.Add(_random.PickAndTake(prefList));
        }

        return result;
    }

    public bool MakeCultist(ICommonSession cultist)
    {
        var cultistRule = EntityQuery<CultRuleComponent>().FirstOrDefault();

        if (cultistRule == null)
        {
            GameTicker.StartGameRule(CultRuleComponent.CultGamePresetPrototype, out var ruleEntity);
            cultistRule = Comp<CultRuleComponent>(ruleEntity);
        }

        if (!_mindSystem.TryGetMind(cultist, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked cultist.");
            return false;
        }

        if (mind.OwnedEntity is not { } playerEntity)
        {
            Log.Error("Mind picked for cultist did not have an attached entity.");
            return false;
        }

        var cultistComponent = new CultistRoleComponent
        {
            PrototypeId = cultistRule.CultistRolePrototype
        };

        _roleSystem.MindAddRole(mindId, cultistComponent);
        EnsureComp<CultistComponent>(playerEntity);

        _factionSystem.RemoveFaction(playerEntity, "NanoTrasen", false);
        _factionSystem.AddFaction(playerEntity, "Cultist");

        if (_inventorySystem.TryGetSlotEntity(playerEntity, "back", out var backPack))
        {
            foreach (var itemPrototype in cultistRule.StartingItems)
            {
                var itemEntity = Spawn(itemPrototype, Transform(playerEntity).Coordinates);

                if (backPack != null)
                {
                    _storageSystem.Insert(backPack.Value, itemEntity, out _);
                }
            }
        }

        // Notificate player about new role assignment
        if (_mindSystem.TryGetSession(mindId, out var session))
        {
            _audioSystem.PlayGlobal(cultistRule.GreatingsSound, session);
            _chatManager.DispatchServerMessage(session, Loc.GetString("cult-role-greeting"));
        }

        _mindSystem.TryAddObjective(mindId, mind, "KillCultTargetObjective");

        return true;
    }

    private void OnNarsieSummon(CultNarsieSummoned ev)
    {
        foreach (var rule in EntityQuery<CultRuleComponent>())
        {
            rule.WinCondition = CultWinCondition.CultWin;
        }

        _roundEndSystem.EndRound();

        var query = EntityQueryEnumerator<MobStateComponent, MindContainerComponent, CultistComponent>();

        while (query.MoveNext(out var uid, out _, out var mindContainer, out _))
        {
            if (!mindContainer.HasMind || mindContainer.Mind is null)
            {
                continue;
            }

            var reaper = Spawn(CultRuleComponent.ReaperPrototype, Transform(uid).Coordinates);
            _mindSystem.TransferTo(mindContainer.Mind.Value, reaper);

            _bodySystem.GibBody(uid);
        }
    }
}
