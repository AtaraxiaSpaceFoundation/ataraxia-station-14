using System.Linq;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Changeling;
using Content.Shared.GameTicking;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Changeling;

public sealed class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly ChangelingNameGenerator _nameGenerator = default!;

    private const int PlayersPerChangeling = 15;
    private const int MaxChangelings = 4;

    private const float ChangelingStartDelay = 3f * 60;
    private const float ChangelingStartDelayVariance = 3f * 60;

    private const int ChangelingMinPlayers = 10;

    private const int ChangelingMaxDifficulty = 5;
    private const int ChangelingMaxPicks = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(ClearUsedNames);

        SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    protected override void ActiveTick(
        EntityUid uid,
        ChangelingRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus == ChangelingRuleComponent.SelectionState.ReadyToSelect &&
            _gameTiming.CurTime > component.AnnounceAt)
            DoChangelingStart(component);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (!ev.Forced && ev.Players.Length < ChangelingMinPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("changeling-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", ChangelingMinPlayers)));

                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("changeling-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    private void DoChangelingStart(ChangelingRuleComponent component)
    {
        if (component.StartCandidates.Count == 0)
        {
            Log.Error("Tried to start Changeling mode without any candidates.");
            return;
        }

        var numChangelings =
            MathHelper.Clamp(component.StartCandidates.Count / PlayersPerChangeling, 1, MaxChangelings);

        var changelingPool =
            _antagSelection.FindPotentialAntags(component.StartCandidates, component.ChangelingPrototypeId);

        var selectedChangelings = _antagSelection.PickAntag(numChangelings, changelingPool);

        foreach (var changeling in selectedChangelings)
        {
            MakeChangeling(changeling);
        }

        component.SelectionStatus = ChangelingRuleComponent.SelectionState.SelectionMade;
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var changeling, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                changeling.StartCandidates[player] = ev.Profiles[player.UserId];
            }

            var delay = TimeSpan.FromSeconds(ChangelingStartDelay +
                _random.NextFloat(0f, ChangelingStartDelayVariance));

            changeling.AnnounceAt = _gameTiming.CurTime + delay;

            changeling.SelectionStatus = ChangelingRuleComponent.SelectionState.ReadyToSelect;
        }
    }

    public bool MakeChangeling(ICommonSession changeling, bool giveObjectives = true)
    {
        var changelingRule = EntityQuery<ChangelingRuleComponent>().FirstOrDefault();
        if (changelingRule == null)
        {
            GameTicker.StartGameRule("Changeling", out var ruleEntity);
            changelingRule = Comp<ChangelingRuleComponent>(ruleEntity);
        }

        if (!_mindSystem.TryGetMind(changeling, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked changeling.");
            return false;
        }

        if (HasComp<ChangelingRoleComponent>(mindId))
        {
            Log.Error($"Player {changeling.Name} is already a changeling.");
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Log.Error("Mind picked for changeling did not have an attached entity.");
            return false;
        }

        _roleSystem.MindAddRole(mindId, new ChangelingRoleComponent
        {
            PrototypeId = changelingRule.ChangelingPrototypeId
        }, mind);

        var briefing = Loc.GetString("changeling-role-briefing-short");

        _roleSystem.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = briefing
        }, mind, true);

        _roleSystem.MindPlaySound(mindId, changelingRule.GreetSoundNotification, mind);
        SendChangelingBriefing(mindId);
        changelingRule.ChangelingMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(entity, "NanoTrasen", false);
        _npcFaction.AddFaction(entity, "Syndicate");

        EnsureComp<ChangelingComponent>(entity, out var readyChangeling);

        readyChangeling.HiveName = _nameGenerator.GetName();
        Dirty(entity, readyChangeling);

        if (!giveObjectives)
            return true;

        var difficulty = 0f;
        for (var pick = 0; pick < ChangelingMaxPicks && ChangelingMaxDifficulty > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "ChangelingObjectiveGroups");
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
        }

        return true;
    }

    private void SendChangelingBriefing(EntityUid mind)
    {
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;

        _chatManager.DispatchServerMessage(session, Loc.GetString("changeling-role-greeting"));
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var changeling, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (changeling.TotalChangelings >= MaxChangelings)
                continue;

            if (!ev.LateJoin)
                continue;

            if (!ev.Profile.AntagPreferences.Contains(changeling.ChangelingPrototypeId))
                continue;

            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                continue;

            if (!job.CanBeAntag)
                continue;

            // Before the announcement is made, late-joiners are considered the same as players who readied.
            if (changeling.SelectionStatus < ChangelingRuleComponent.SelectionState.SelectionMade)
            {
                changeling.StartCandidates[ev.Player] = ev.Profile;
                continue;
            }

            var target = PlayersPerChangeling * changeling.TotalChangelings + 1;

            var chance = 1f / PlayersPerChangeling;

            if (ev.JoinOrder < target)
            {
                chance /= (target - ev.JoinOrder);
            }
            else
            {
                chance *= ev.JoinOrder + 1 - target;
            }

            if (chance > 1)
                chance = 1;

            if (_random.Prob(chance))
            {
                MakeChangeling(ev.Player);
            }
        }
    }

    private void OnObjectivesTextGetInfo(
        EntityUid uid,
        ChangelingRuleComponent comp,
        ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.ChangelingMinds;
        args.AgentName = Loc.GetString("changeling-round-end-agent-name");
    }

    private void ClearUsedNames(RoundRestartCleanupEvent ev)
    {
        _nameGenerator.ClearUsed();
    }
}
