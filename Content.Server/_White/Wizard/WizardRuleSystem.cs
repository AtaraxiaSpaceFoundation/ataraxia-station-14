using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.NPC.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.Objectives;
using Content.Server.Station.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Components;
using Content.Shared.Objectives.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Random;

namespace Content.Server._White.Wizard;

/// <summary>
/// This handles...
/// </summary>
public sealed class WizardRuleSystem : GameRuleSystem<WizardRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<WizardComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<WizardComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WizardComponent, GhostRoleSpawnerUsedEvent>(OnPlayersGhostSpawning);
        SubscribeLocalEvent<WizardComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<WizardRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

        _sawmill = _logManager.GetSawmill("Wizard");
    }

    private void OnObjectivesTextGetInfo(Entity<WizardRuleComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = ent.Comp.WizardMinds;
        args.AgentName = Loc.GetString("wizard-round-end-agent-name");
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, Loc.GetString("wizard-title"));
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var wizardRule, out _))
        {
            if (!SpawnMap((uid, wizardRule)))
            {
                _sawmill.Info("Failed to load shuttle for wizard");
                continue;
            }

            //Handle there being nobody readied up
            if (ev.PlayerPool.Count == 0)
                continue;

            var wizardEligible =
                _antagSelection.GetEligibleSessions(ev.PlayerPool, wizardRule.WizardRoleProto);

            //Select wizard
            var selectedWizard = _antagSelection
                .ChooseAntags(1, wizardEligible, ev.PlayerPool).FirstOrDefault();

            SpawnWizard(selectedWizard, wizardRule, false);

            if (selectedWizard != null)
                GameTicker.PlayerJoinGame(selectedWizard);
        }
    }

    protected override void Started(
        EntityUid uid,
        WizardRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (GameTicker.RunLevel == GameRunLevel.InRound)
            SpawnWizardGhostRole(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, WizardComponent component, ComponentRemove args)
    {
        CheckAnnouncement();
    }

    private void OnMobStateChanged(EntityUid uid, WizardComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            CheckAnnouncement();
    }

    private void OnPlayersGhostSpawning(EntityUid uid, WizardComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var spawner = args.Spawner;

        if (!TryComp<WizardSpawnerComponent>(spawner, out var wizardSpawner))
            return;

        HumanoidCharacterProfile? profile = null;
        if (TryComp(args.Spawned, out ActorComponent? actor))
            profile = _prefs.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter as HumanoidCharacterProfile;

        if (!EntityQuery<WizardRuleComponent>().Any())
            return;

        if (!_prototypeManager.TryIndex(wizardSpawner.StartingGear, out var gear))
        {
            _sawmill.Error("Failed to load wizard gear prototype");
            return;
        }

        SetupWizardEntity(uid, wizardSpawner.Points, gear, profile);
    }

    private void OnMindAdded(EntityUid uid, WizardComponent component, MindAddedMessage args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var wizardRule, out _))
        {
            AddRole(mindId, mind, wizardRule);

            if (mind.Session is not { } playerSession)
                return;

            if (GameTicker.RunLevel != GameRunLevel.InRound)
                return;

            NotifyWizard(playerSession, component, wizardRule);
        }
    }

    private void AddRole(EntityUid mindId, MindComponent mind, WizardRuleComponent wizardRule)
    {
        if (_roles.MindHasRole<WizardRoleComponent>(mindId))
            return;

        wizardRule.WizardMinds.Add(mindId);

        var role = wizardRule.WizardRoleProto;
        _roles.MindAddRole(mindId, new WizardRoleComponent {PrototypeId = role});

        GiveObjectives(mindId, mind, wizardRule);
    }

    private void GiveObjectives(EntityUid mindId, MindComponent mind, WizardRuleComponent wizardRule)
    {
        _mind.TryAddObjective(mindId, mind, "WizardSurviveObjective");

        var difficulty = 0f;
        for (var pick = 0; pick < 6 && 8 > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, wizardRule.ObjectiveGroup);
            if (objective == null)
                continue;

            _mind.AddObjective(mindId, mind, objective.Value);
            var adding = Comp<ObjectiveComponent>(objective.Value).Difficulty;
            difficulty += adding;
            _sawmill.Debug($"Added objective {ToPrettyString(objective):objective} with {adding} difficulty");
        }
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var wiz, out _))
        {
            if (ev.New == GameRunLevel.InRound)
                OnRoundStart(uid, wiz);
        }
    }

    private void OnRoundStart(EntityUid uid, WizardRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var eligible = new List<Entity<StationEventEligibleComponent, NpcFactionMemberComponent>>();
        var eligibleQuery = EntityQueryEnumerator<StationEventEligibleComponent, NpcFactionMemberComponent>();
        while (eligibleQuery.MoveNext(out var eligibleUid, out var eligibleComp, out var member))
        {
            if (!_npcFaction.IsFactionHostile(component.Faction, (eligibleUid, member)))
                continue;

            eligible.Add((eligibleUid, eligibleComp, member));
        }

        if (eligible.Count == 0)
            return;

        component.TargetStation = _random.Pick(eligible);

        var filter = Filter.Empty();
        var query = EntityQueryEnumerator<WizardComponent, ActorComponent>();
        while (query.MoveNext(out _, out var wizard, out var actor))
        {
            NotifyWizard(actor.PlayerSession, wizard, component);
            filter.AddPlayer(actor.PlayerSession);
        }
    }

    private void CheckAnnouncement()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var wizard, out _))
        {
            _roundEndSystem.DoRoundEndBehavior(
                wizard.RoundEndBehavior, wizard.EvacShuttleTime, wizard.RoundEndTextSender,
                wizard.RoundEndTextShuttleCall, wizard.RoundEndTextAnnouncement);

            return;
        }
    }

    private bool SpawnMap(Entity<WizardRuleComponent> ent)
    {
        if (!ent.Comp.SpawnShuttle
            || ent.Comp.ShuttleMap != null)
            return true;

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        if (!_map.TryLoad(shuttleMap, ent.Comp.ShuttlePath, out _, options))
            return false;

        ent.Comp.ShuttleMap = _mapManager.GetMapEntityId(shuttleMap);
        return true;
    }

    private void SetupWizardEntity(
        EntityUid mob,
        int points,
        StartingGearPrototype gear,
        HumanoidCharacterProfile? profile)
    {
        EnsureComp<WizardComponent>(mob);

        profile ??= HumanoidCharacterProfile.RandomWithSpecies();

        _humanoid.LoadProfile(mob, profile);

        _metaData.SetEntityName(mob, profile.Name);

        _stationSpawning.EquipStartingGear(mob, gear, profile);

        _npcFaction.RemoveFaction(mob, "NanoTrasen", false);
        _npcFaction.AddFaction(mob, "Wizard");
    }

    private void SpawnWizard(ICommonSession? session, WizardRuleComponent component, bool spawnGhostRoles = true)
    {
        if (component.ShuttleMap is not {Valid: true} mapUid)
            return;

        var spawn = new EntityCoordinates();
        foreach (var (_, meta, xform) in EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != component.SpawnPointProto.Id)
                continue;

            if (xform.ParentUid != component.ShuttleMap)
                continue;

            spawn = xform.Coordinates;
            break;
        }

        //Fallback, spawn at the centre of the map
        if (spawn == new EntityCoordinates())
        {
            spawn = Transform(mapUid).Coordinates;
            _sawmill.Warning("Fell back to default spawn for wizard!");
        }

        var wizardAntag = _prototypeManager.Index(component.WizardRoleProto);

        //If a session is available, spawn mob and transfer mind into it
        if (session != null)
        {
            var profile =
                _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;

            profile ??= HumanoidCharacterProfile.RandomWithSpecies();
            var name = profile.Name;

            if (!_prototypeManager.TryIndex(profile.Species, out SpeciesPrototype? species))
            {
                species = _prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies);
            }

            var mob = Spawn(species.Prototype, spawn);
            if (!_prototypeManager.TryIndex(component.StartingGear, out var gear))
            {
                _sawmill.Error("Failed to load wizard gear prototype");
                return;
            }

            SetupWizardEntity(mob, component.Points, gear, profile);

            var newMind = _mind.CreateMind(session.UserId, name);
            _mind.SetUserId(newMind, session.UserId);
            AddRole(newMind.Owner, newMind.Comp, component);

            _mind.TransferTo(newMind, mob);
        }
        //Otherwise, spawn as a ghost role
        else if (spawnGhostRoles)
        {
            var spawnPoint = Spawn(component.GhostSpawnPointProto, spawn);
            var ghostRole = EnsureComp<GhostRoleComponent>(spawnPoint);
            EnsureComp<GhostRoleMobSpawnerComponent>(spawnPoint);
            ghostRole.RoleName = Loc.GetString(wizardAntag.Name);
            ghostRole.RoleDescription = Loc.GetString(wizardAntag.Objective);

            var wizardSpawner = EnsureComp<WizardSpawnerComponent>(spawnPoint);
            //TODO: maybe other params
        }
    }

    private void NotifyWizard(ICommonSession session, WizardComponent wizard, WizardRuleComponent wizardRule)
    {
        if (wizardRule.TargetStation is not { } station)
            return;

        _antagSelection.SendBriefing(session, Loc.GetString("wizard-welcome", ("station", station)), Color.Aqua, null);
    }

    /// <summary>
    /// Spawn wizard ghost role if this gamerule was started mid round
    /// </summary>
    private void SpawnWizardGhostRole(EntityUid uid, WizardRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!SpawnMap((uid, component)))
        {
            _sawmill.Info("Failed to load map for wizard");
            return;
        }

        ICommonSession? session = null;
        SpawnWizard(session, component, true);
    }
}
