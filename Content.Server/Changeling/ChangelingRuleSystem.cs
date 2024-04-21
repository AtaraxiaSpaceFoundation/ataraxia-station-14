using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared._White.Mood;
using Content.Shared.Changeling;
using Content.Shared.GameTicking;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Changeling;

public sealed class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly ChangelingNameGenerator _nameGenerator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const int PlayersPerChangeling = 15;
    private const int MaxChangelings = 4;

    private const float ChangelingStartDelay = 3f * 60;
    private const float ChangelingStartDelayVariance = 3f * 60;

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

        SubscribeLocalEvent<ChangelingRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    protected override void Added(EntityUid uid, ChangelingRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        gameRule.MinPlayers = PlayersPerChangeling;
    }

    private void OnGetBriefing(Entity<ChangelingRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("changeling-role-briefing-short"));
    }

    protected override void ActiveTick(
        EntityUid uid,
        ChangelingRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus < ChangelingRuleComponent.SelectionState.Started &&
            component.AnnounceAt < _gameTiming.CurTime)
        {
            DoChangelingStart(component);
            component.SelectionStatus = ChangelingRuleComponent.SelectionState.Started;
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, Loc.GetString("changeling-title"));
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var changeling, out _))
        {
            var delay = TimeSpan.FromSeconds(ChangelingStartDelay +
                _random.NextFloat(0f, ChangelingStartDelayVariance));

            changeling.AnnounceAt = _gameTiming.CurTime + delay;

            changeling.SelectionStatus = ChangelingRuleComponent.SelectionState.ReadyToStart;
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var changeling, out _))
        {
            if (changeling.TotalChangelings >= MaxChangelings)
                continue;

            if (!ev.LateJoin)
                continue;

            if (!_antagSelection.IsPlayerEligible(ev.Player, changeling.ChangelingPrototypeId))
                continue;

            // Before the announcement is made, late-joiners are considered the same as players who readied.
            if (changeling.SelectionStatus < ChangelingRuleComponent.SelectionState.Started)
                continue;

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
                MakeChangeling(ev.Mob, changeling);
            }
        }
    }

    private void ClearUsedNames(RoundRestartCleanupEvent ev)
    {
        _nameGenerator.ClearUsed();
    }

    private void OnObjectivesTextGetInfo(
        EntityUid uid,
        ChangelingRuleComponent comp,
        ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.ChangelingMinds;
        args.AgentName = Loc.GetString("changeling-round-end-agent-name");
    }

    private void DoChangelingStart(ChangelingRuleComponent component)
    {
        var eligiblePlayers =
            _antagSelection.GetEligiblePlayers(_playerManager.Sessions, component.ChangelingPrototypeId);

        if (eligiblePlayers.Count == 0)
        {
            return;
        }

        var changelingsToSelect =
            _antagSelection.CalculateAntagCount(_playerManager.PlayerCount, PlayersPerChangeling, MaxChangelings);

        var selectedChangelings = _antagSelection.ChooseAntags(changelingsToSelect, eligiblePlayers);

        foreach (var changeling in selectedChangelings)
        {
            MakeChangeling(changeling, component);
        }
    }

    public void AdminMakeChangeling(EntityUid entity)
    {
        var changelingRule = EntityQuery<ChangelingRuleComponent>().FirstOrDefault();
        if (changelingRule == null)
        {
            GameTicker.StartGameRule("Changeling", out var ruleEntity);
            changelingRule = Comp<ChangelingRuleComponent>(ruleEntity);
        }

        if (HasComp<ChangelingRuleComponent>(entity))
            return;

        MakeChangeling(entity, changelingRule);
    }

    public bool MakeChangeling(EntityUid changeling, ChangelingRuleComponent rule, bool giveObjectives = true)
    {
        if (!_mindSystem.TryGetMind(changeling, out var mindId, out var mind))
        {
            return false;
        }

        if (HasComp<ChangelingRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is already a changeling.");
            return false;
        }

        var briefing = Loc.GetString("changeling-role-greeting");
        _antagSelection.SendBriefing(changeling, briefing, null, rule.GreetSoundNotification);

        rule.ChangelingMinds.Add(mindId);

        _roleSystem.MindAddRole(mindId, new ChangelingRoleComponent
        {
            PrototypeId = rule.ChangelingPrototypeId
        }, mind);

        // Change the faction
        _npcFaction.RemoveFaction(changeling, "NanoTrasen", false);
        _npcFaction.AddFaction(changeling, "Syndicate");

        EnsureComp<ChangelingComponent>(changeling, out var readyChangeling);

        readyChangeling.HiveName = _nameGenerator.GetName();
        Dirty(changeling, readyChangeling);

        RaiseLocalEvent(changeling, new MoodEffectEvent("TraitorFocused"));

        if (!giveObjectives)
            return true;

        var difficulty = 0f;
        for (var pick = 0; pick < ChangelingMaxPicks && ChangelingMaxDifficulty > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "ChangelingObjectiveGroups");
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            var adding = Comp<ObjectiveComponent>(objective.Value).Difficulty;
            difficulty += adding;
            Log.Debug($"Added objective {ToPrettyString(objective):objective} with {adding} difficulty");
        }

        return true;
    }
}
