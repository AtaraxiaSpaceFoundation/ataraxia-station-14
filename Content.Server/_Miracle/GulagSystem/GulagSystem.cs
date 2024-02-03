using System.Linq;
using Content.Server._Miracle.Components;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.KillTracking;
using Content.Server.Materials;
using Content.Server.Mind;
using Content.Server.Parallax;
using Content.Server.Preferences.Managers;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Miracle.Cvars;
using Content.Shared._Miracle.GulagSystem;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Miracle.GulagSystem;

public sealed partial class GulagSystem : SharedGulagSystem
{
    //1 second = 10 points
    [Dependency] private readonly AdminSystem _adminSystem = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSpawningSystem _spawningSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorageSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;


    private readonly List<ProtoId<BiomeTemplatePrototype>> _gulagBiomes = new()
    {
        "GulagBiome"
    };

    private readonly List<string> _gulagMaps = new()
    {
        "/Maps/Gulags/gulag.yml"
    };

    private double _timeToPointsRatio;

    private MapId? _activeMap;
    private EntityUid? _mapEntity;

    private readonly TimeSpan _safeguardUpdateRate = TimeSpan.FromSeconds(10);
    private DateTime _nextSafeguardUpdate = DateTime.MinValue;

    private readonly TimeSpan _shuttleFillUpdateRate = TimeSpan.FromMinutes(10);
    private DateTime _nextShuttleFillUpdate = DateTime.MinValue;

    private List<EntityCoordinates> _spawnCoords = new();

    private readonly Dictionary<NetUserId, double> _pointsPerPlayer = new();
    private readonly Dictionary<ProtoId<MaterialPrototype>, int> _gulagMaterialStorage = new();

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(MiracleCvars.GulagPointsToTimeRatio, newValue => _timeToPointsRatio = newValue, true);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(BeforeSpawn);

        SubscribeLocalEvent<GulagOreProcessorComponent, MaterialEntityInsertedEvent>(OnOreInserted);
        SubscribeLocalEvent<GulagOreProcessorComponent, InteractUsingEvent>(OnInteract, before: new[] {typeof(MaterialStorageSystem)});

        SubscribeLocalEvent<GulagFillContainerComponent, MapInitEvent>(OnGulagContainerSpawned);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);

        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnJoinedLobby);
        //safeguard
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        if(IsUserGulaged(ev.PlayerSession.UserId, out _))
        {
            _chatManager.DispatchServerMessage(ev.PlayerSession, Loc.GetString("gulag-chat-join-message"));
        }
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        if (!HasComp<GulagBoundComponent>(ev.Entity))
        {
            return;
        }

        if (ev.Primary is not KillPlayerSource source)
        {
            return;
        }

        var player = source.PlayerId;

        if (!IsUserGulaged(player, out var ban))
        {
            return;
        }

        var banDef = ban.First();
        var newExpirationTime = banDef.ExpirationTime!.Value.DateTime + TimeSpan.FromDays(1);
        _db.EditServerBan(banDef.Id!.Value, banDef.Reason, banDef.Severity, newExpirationTime, banDef.UserId!.Value, DateTime.Now);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        Safeguard();
        TryFillCargoShuttle();
    }

    private void OnGulagContainerSpawned(EntityUid uid, GulagFillContainerComponent component, MapInitEvent args)
    {
        var coords = Transform(uid).Coordinates;

        foreach (var (materialId, value) in _gulagMaterialStorage)
        {
            var materialEntities = _materialStorageSystem.SpawnMultipleFromMaterial(value, materialId, coords);

            foreach (var material in materialEntities)
            {
                _entityStorageSystem.Insert(material, uid);
            }
        }

        _gulagMaterialStorage.Clear();
    }

    private void TryFillCargoShuttle()
    {
        if (_nextShuttleFillUpdate > DateTime.Now)
        {
            return;
        }

        if (_gulagMaterialStorage.Count == 0)
        {
            return;
        }

        var station = GetMainStation();

        if (!station.HasValue)
        {
            return;
        }

        if (!TryComp<StationCargoOrderDatabaseComponent>(station.Value, out var comp))
        {
            return;
        }

        _cargoSystem.AddAndApproveOrder(station.Value, "CrateGulag", 0, 1, Loc.GetString("gulag-sender"),
            Loc.GetString("gulag-order-description"), Loc.GetString("gulag-order-destination"), comp);

        _nextShuttleFillUpdate = DateTime.Now + _shuttleFillUpdateRate;
    }

    // Just check if we need to bring back somehow escaped players
    private void Safeguard()
    {
        if (_nextSafeguardUpdate > DateTime.Now)
        {
            return;
        }

        var querry = EntityQueryEnumerator<GulagBoundComponent, TransformComponent>();

        while (querry.MoveNext(out var uid, out var gulagbound, out var xform))
        {
            if (xform.MapID != _activeMap)
            {
                SendToGulag(uid);
            }
        }

        _nextSafeguardUpdate = DateTime.Now + _safeguardUpdateRate;
    }

    public void SendToGulag(ICommonSession session)
    {
        var playerEntity = session.AttachedEntity;

        if (_mapEntity == null)
        {
            _adminSystem.Erase(session);
            return;
        }

        if (playerEntity.HasValue)
        {
            SendToGulag(playerEntity.Value);
        }
        else
        {
            SpawnPlayer(session, (HumanoidCharacterProfile)_preferencesManager.GetPreferences(session.UserId).SelectedCharacter);
        }

        var banDef = _banManager.GetServerBans(session.UserId).First();
        var message = Loc.GetString("gulag-greetings-message", ("BanTime", $"{(banDef.ExpirationTime! - DateTime.Now).Value.TotalHours}"));

        _chatManager.DispatchServerMessage(session, message);
    }

    private void OnInteract(EntityUid uid, GulagOreProcessorComponent component, InteractUsingEvent args)
    {
        //It wasn't player who interacted with the entity
        if (!_playerManager.TryGetSessionByEntity(args.User, out var session))
        {
            return;
        }

        component.LastInteractedUser = session.UserId;
        Log.Info("OnInteract raised");
    }

    private void OnOreInserted(EntityUid uid, GulagOreProcessorComponent component, MaterialEntityInsertedEvent args)
    {
        var storageComponent = Comp<MaterialStorageComponent>(uid);
        var userId = component.LastInteractedUser!.Value;

        foreach (var (materialId, currentVolume ) in storageComponent.Storage)
        {
            var materialPrototype = _prototypeManager.Index<MaterialPrototype>(materialId.Id);
            var stackVolume = _materialStorageSystem.GetSheetVolume(materialPrototype);
            var actualOreCount = currentVolume / stackVolume;

            var points = materialPrototype.Price * actualOreCount;

            _pointsPerPlayer[userId] = points + _pointsPerPlayer.GetValueOrDefault(userId);

            _gulagMaterialStorage[materialId] = currentVolume + _gulagMaterialStorage.GetValueOrDefault(materialId);
            _materialStorageSystem.TrySetMaterialAmount(uid, materialId, 0);

        }

        var time = ConvertPointsToTime(_pointsPerPlayer[userId]);
        _popupSystem.PopupEntity(Loc.GetString("gulag-ban-time-changed", ("Time", $"{time.TotalSeconds}")), uid, PopupType.Medium);
    }

    public bool IsUserGulaged(NetUserId playerId, out HashSet<ServerBanDef> bans)
    {
        bans = _banManager.GetServerBans(playerId);

        return bans.Count != 0;
    }

    private void SendToGulag(EntityUid playerEntity)
    {
        if (_inventorySystem.TryGetContainerSlotEnumerator(playerEntity, out var enumerator))
        {
            while (enumerator.NextItem(out var item, out var slot))
            {
                if (_inventorySystem.TryUnequip(playerEntity, playerEntity, slot.Name, true, true))
                    _physicsSystem.ApplyAngularImpulse(item, ThrowingSystem.ThrowAngularImpulse);
            }
        }

        if (TryComp(playerEntity, out HandsComponent? hands))
        {
            foreach (var hand in _handsSystem.EnumerateHands(playerEntity, hands))
            {
                _handsSystem.TryDrop(playerEntity, hand, checkActionBlocker: false, doDropInteraction: false,
                    handsComp: hands);
            }
        }

        var newPosition = GetSpawnPosition();

        _transformSystem.SetCoordinates(playerEntity, newPosition);
        _transformSystem.AttachToGridOrMap(playerEntity);

        EnsureComp<GulagBoundComponent>(playerEntity);
        EnsureComp<KillTrackerComponent>(playerEntity);
    }

    private void SpawnPlayer(ICommonSession session, HumanoidCharacterProfile profile)
    {
        var newMind = _mind.CreateMind(session.UserId, profile.Name);
        _mind.SetUserId(newMind, session.UserId);

        var coords = GetSpawnPosition();
        var mob = _spawningSystem.SpawnPlayerMob(coords, null, profile, null);

        _mind.TransferTo(newMind, mob);

        EnsureComp<GulagBoundComponent>(mob);
        EnsureComp<KillTrackerComponent>(mob);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        var bans = _banManager.GetServerBans(ev.Player.UserId);

        if (bans.Count == 0)
        {
            return;
        }

        var xform = Transform(ev.Entity);

        if (xform.MapID != _activeMap)
        {
            SendToGulag(ev.Player);
        }
    }

    private void BeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var bans = _banManager.GetServerBans(ev.Player.UserId);

        if (bans.Count == 0)
        {
            return;
        }

        ev.Handled = true;
        SendToGulag(ev.Player);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        foreach (var (player, points) in _pointsPerPlayer)
        {
            var banDef = _banManager.GetServerBans(player).FirstOrDefault();
            if (banDef == null)
            {
                continue;
            }

            var newExpirationTime = banDef.ExpirationTime!.Value.DateTime - ConvertPointsToTime(points);

            _db.EditServerBan(banDef.Id!.Value, banDef.Reason, banDef.Severity, newExpirationTime, banDef.UserId!.Value, DateTime.Now);
        }

        _activeMap = null!;
        _mapEntity = null!;
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        //Spawn Gulag
        var mapId = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(mapId);

        _metaData.SetEntityName(mapUid, "Gulag Map");

        _mapManager.AddUninitializedMap(mapId);

        var pickedMap = _random.Pick(_gulagMaps);
        if (!_mapLoader.TryLoad(mapId, pickedMap, out var uids))
        {
            _mapManager.DeleteMap(mapId);
            Log.Error("Can't spawn map with path {0}", pickedMap);
            return;
        }

        foreach (var uid in uids)
        {
            _metaData.SetEntityName(uid, $"Gulag grid {uid}");
        }

        var pickerBiome = _random.Pick(_gulagBiomes);
        _biome.EnsurePlanet(mapUid, _prototypeManager.Index<BiomeTemplatePrototype>(pickerBiome));

        _mapManager.DoMapInitialize(mapId);
        _activeMap = mapId;
        _mapEntity = mapUid;


        //Item2 = TransformComponent
        _spawnCoords = EntityQuery<SpawnPointComponent, TransformComponent>()
            .Where(x => x.Item2.MapID == mapId)
            .Select(x => x.Item2.Coordinates)
            .ToList();
    }

    private TimeSpan ConvertPointsToTime(double points)
    {
        return TimeSpan.FromSeconds(points / _timeToPointsRatio);
    }

    private EntityCoordinates GetSpawnPosition()
    {
        return _spawnCoords.Count != 0 ? _random.Pick(_spawnCoords) : Transform(_mapEntity!.Value).Coordinates;
    }

    private EntityUid? GetMainStation()
    {
        var stations = _stationSystem.GetStations();

        foreach (var station in stations)
        {
            var stationData = Comp<StationDataComponent>(station);

            if (!HasComp<StationCargoOrderDatabaseComponent>(station))
            {
                continue;
            }

            foreach (var grid in stationData.Grids)
            {
                if (Transform(grid).MapID == _gameTicker.DefaultMap)
                {
                    return station;
                }
            }
        }

        return null!;
    }
}
