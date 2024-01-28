using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Systems;
using Content.Server.Roles;
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
using Content.Shared.Objectives;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared._White.Cult;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared._White;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.GameRule;

public sealed class CultRuleSystem : GameRuleSystem<CultRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly JobSystem _jobSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

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
        var querry = EntityQueryEnumerator<CultRuleComponent, GameRuleComponent>();

        while (querry.MoveNext(out _, out var cultRuleComponent, out _))
        {
            if (cultRuleComponent.CultTarget.HasValue && TryComp<MindComponent>(cultRuleComponent.CultTarget.Value, out var mind))
            {
                return mind;
            }
        }

        return null!;
    }

    public bool CanSummonNarsie()
    {
        var querry = EntityQueryEnumerator<CultRuleComponent, GameRuleComponent>();

        while (querry.MoveNext(out _, out var cultRuleComponent, out _))
        {
            var cultistsAmount = cultRuleComponent.Cultists.Count;
            var constructsAmount = cultRuleComponent.Constructs.Count;
            var enoughCultists = cultistsAmount + constructsAmount > 10;

            if (!enoughCultists)
            {
                return false;
            }

            var target = GetTarget();
            var targetKilled = target == null || _mindSystem.IsCharacterDeadIc(target);

            if (targetKilled)
                return true;
        }

        return false;
    }

    private void CheckRoundShouldEnd()
    {
        var querry = EntityQueryEnumerator<CultRuleComponent, GameRuleComponent>();
        var aliveCultistsCount = 0;

        while (querry.MoveNext(out _, out var cultRuleComponent, out _))
        {
            var cultists = 0;
            foreach (var cultistComponent in cultRuleComponent.Cultists)
            {
                var owner = cultistComponent.Owner;
                if (!TryComp<MobStateComponent>(owner, out var mobState))
                    continue;

                if (_mobStateSystem.IsAlive(owner, mobState))
                {
                    cultists++;
                }
            }

            if (cultists == 0)
                cultRuleComponent.WinCondition = CultWinCondition.CultFailure;

            aliveCultistsCount += cultists;
        }

        if (aliveCultistsCount == 0)
        {
            _roundEndSystem.EndRound();
        }
    }

    private void OnCultistComponentInit(EntityUid uid, CultistComponent component, ComponentInit args)
    {
        var query = EntityQueryEnumerator<CultRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var ruleEnt, out var cultRuleComponent, out _))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEnt))
                continue;

            if (!TryComp<MindContainerComponent>(uid, out var mindComponent))
                return;

            if (!mindComponent.HasMind)
                return;

            cultRuleComponent.Cultists.Add(component);

            if (TryComp<ActorComponent>(component.Owner, out var actor))
            {
                cultRuleComponent.CultistsList.Add(MetaData(component.Owner).EntityName, actor.PlayerSession.Name);
            }

            var traitorRole = new TraitorRoleComponent()
            {
                PrototypeId = cultRuleComponent.CultistRolePrototype
            };

            _roleSystem.MindAddRole(mindComponent.Mind.Value, traitorRole);

            UpdateCultistsAppearance(cultRuleComponent);
        }
    }

    private void OnCultistComponentRemoved(EntityUid uid, CultistComponent component, ComponentRemove args)
    {
        var query = EntityQueryEnumerator<CultRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var ruleEnt, out var cultRuleComponent, out _))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEnt))
                continue;

            cultRuleComponent.Cultists.Remove(component);

            RemoveCultistAppearance(component);

            CheckRoundShouldEnd();
        }
    }

    private void RemoveCultistAppearance(CultistComponent component)
    {
        if (TryComp<HumanoidAppearanceComponent>(component.Owner, out var appearanceComponent))
        {
            //Потому что я так сказал
            appearanceComponent.EyeColor = Color.White;
            Dirty(appearanceComponent);
        }

        RemComp<PentagramComponent>(component.Owner);
    }

    private void UpdateCultistsAppearance(CultRuleComponent cultRuleComponent)
    {
        var cultistsCount = cultRuleComponent.Cultists.Count;
        var constructsCount = cultRuleComponent.Constructs.Count;
        var totalCultMembers = cultistsCount + constructsCount;
        if (totalCultMembers < CultRuleComponent.ReadEyeThreshold)
            return;

        foreach (var cultistComponent in cultRuleComponent.Cultists)
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
        var querry = EntityQuery<CultRuleComponent>();

        foreach (var cultRuleComponent in querry)
        {
            var winText = Loc.GetString($"cult-cond-{cultRuleComponent.WinCondition.ToString().ToLower()}");
            ev.AddLine(winText);

            ev.AddLine(Loc.GetString("cultists-list-start"));

            foreach (var (entityName, ckey) in cultRuleComponent.CultistsList)
            {
                var lising = Loc.GetString("cultists-list-name", ("name", entityName), ("user", ckey));
                ev.AddLine(lising);
            }
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<CultRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = _cultGameRuleMinimapPlayers;
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));

                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<CultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var cultRule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                cultRule.StarCandidates[player] = ev.Profiles[player.UserId];
            }

            var potentialCultists = FindPotentialCultist(cultRule.StarCandidates);
            var pickedCultist = PickCultists(potentialCultists);
            var potentialTargets = FindPotentialTargets(pickedCultist);

            cultRule.CultTarget = _random.PickAndTake(potentialTargets).Mind;

            foreach (var pickerCultist in pickedCultist)
            {
                MakeCultist(pickerCultist);
            }
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

            if (exclude?.Contains(actor.PlayerSession) is true)
            {
                continue;
            }

            potentialTargets.Add(mind);
        }

        return potentialTargets;
    }

    private List<ICommonSession> FindPotentialCultist(in Dictionary<ICommonSession, HumanoidCharacterProfile> candidates)
    {
        var list = new List<ICommonSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            // Role prevents antag.
            if (!_jobSystem.CanBeAntag(player)) continue;

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
        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient ready players to fill up with cultists, stopping the selection.");
            return result;
        }

        var minCultists = _cfg.GetCVar(WhiteCVars.CultMinPlayers);
        var maxCultists = _cfg.GetCVar(WhiteCVars.CultMaxStartingPlayers);

        var actualCultistCount = prefList.Count > maxCultists ? maxCultists : minCultists;

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

        var mind = cultist.Data.ContentData()?.Mind;

        if (mind == null)
        {
            _sawmill.Info("Failed getting mind for picked cultist.");
            return false;
        }

        var playerEntity = cultist.AttachedEntity;

        if (!playerEntity.HasValue)
        {
            _sawmill.Error("Mind picked for cultist did not have an attached entity.");
            return false;
        }

        var mindComponent = Comp<MindComponent>(mind.Value);

        DebugTools.AssertNotNull(playerEntity.Value);
        EnsureComp<CultistComponent>(playerEntity.Value);

        _factionSystem.RemoveFaction(playerEntity.Value, "NanoTrasen", false);
        _factionSystem.AddFaction(playerEntity.Value, "Cultist");

        if (_inventorySystem.TryGetSlotEntity(playerEntity.Value, "back", out var backPack))
        {
            foreach (var itemPrototype in cultistRule.StartingItems)
            {
                var itemEntity = Spawn(itemPrototype, Transform(playerEntity.Value).Coordinates);

                if (backPack != null)
                {
                    _storageSystem.Insert(backPack.Value, itemEntity, out _);
                }
            }
        }

        _audioSystem.PlayGlobal(cultistRule.GreatingsSound, Filter.Empty().AddPlayer(cultist), false,
            AudioParams.Default);

        _chatManager.DispatchServerMessage(cultist, Loc.GetString("cult-role-greeting"));

        _mindSystem.TryAddObjective(mind.Value, mindComponent, "CultistKillObjective");

        return true;
    }

    private void OnNarsieSummon(CultNarsieSummoned ev)
    {
        foreach (var rule in EntityQuery<CultRuleComponent>())
        {
            rule.WinCondition = CultWinCondition.CultWin;
        }

        _roundEndSystem.EndRound();

        var query = EntityQuery<MobStateComponent, MindContainerComponent, CultistComponent>().ToList();

        foreach (var (mobState, mindContainer, _) in query)
        {
            if (!mindContainer.HasMind || mindContainer.Mind is null)
            {
                continue;
            }

            var reaper = Spawn(CultRuleComponent.ReaperPrototype, Transform(mobState.Owner).Coordinates);
            _mindSystem.TransferTo(mindContainer.Mind.Value, reaper);

            _bodySystem.GibBody(mobState.Owner);
        }
    }
}
