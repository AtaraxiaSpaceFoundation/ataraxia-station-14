using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Content.Server._Miracle.GulagSystem;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server._White;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Server._White.PandaSocket.Main;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Managers;

public sealed class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!; // WD
    [Dependency] private readonly PandaWebManager _pandaWeb = default!; // WD
    [Dependency] private readonly IEntityManager _entMan = default!; // WD

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.bans";
    public const string JobPrefix = "Job:";
    public const string UnknownServer = "unknown";

    private readonly Dictionary<NetUserId, HashSet<ServerRoleBanDef>> _cachedRoleBans = new();
    private readonly Dictionary<NetUserId, HashSet<ServerBanDef>> _cachedServerBans = new(); // Miracle edit

    public void Initialize()
    {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        _netManager.RegisterNetMessage<MsgRoleBans>();
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected || _cachedRoleBans.ContainsKey(e.Session.UserId))
            return;

        var netChannel = e.Session.Channel;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        await CacheDbRoleBans(e.Session.UserId, netChannel.RemoteEndPoint.Address, hwId);
        await CacheDbServerBans(e.Session.UserId, netChannel.RemoteEndPoint.Address, hwId); //Miracle edit

        SendRoleBans(e.Session);
    }

    private async Task<bool> AddRoleBan(ServerRoleBanDef banDef)
    {
        banDef = await _db.AddServerRoleBanAsync(banDef);

        if (banDef.UserId != null)
        {
            _cachedRoleBans.GetOrNew(banDef.UserId.Value).Add(banDef);
        }

        return true;
    }

    public HashSet<string>? GetRoleBans(NetUserId playerUserId)
    {
        return _cachedRoleBans.TryGetValue(playerUserId, out var roleBans) ? roleBans.Select(banDef => banDef.Role).ToHashSet() : null;
    }

    private async Task CacheDbRoleBans(NetUserId userId, IPAddress? address = null, ImmutableArray<byte>? hwId = null)
    {
        var roleBans = await _db.GetServerRoleBansAsync(address, userId, hwId, false);

        var userRoleBans = new HashSet<ServerRoleBanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        _cachedRoleBans[userId] = userRoleBans;
    }

    //Miracle edit start
    private async Task CacheDbServerBans(NetUserId userId, IPAddress? address = null, ImmutableArray<byte>? hwId = null)
    {
        var serverBans = await _db.GetServerBansAsync(address, userId, hwId, false);

        var userServerBans = new HashSet<ServerBanDef>(serverBans);

        _cachedServerBans[userId] = userServerBans;
    }
    //Miracle edit end

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new List<NetUserId>();
        foreach (var player in _cachedRoleBans.Keys)
        {
            if (!_playerManager.TryGetSessionById(player, out _))
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
            _cachedServerBans.Remove(player); //Miracle edit
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveWhere(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }

        //Miracle edit start
        foreach (var serverBan in _cachedServerBans.Values)
        {
            serverBan.RemoveWhere(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
        //Miracle edit end
    }

    #region Server Bans
    public async void CreateServerBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableArray<byte>? hwid, uint? minutes, NoteSeverity severity, string reason, bool isGlobalBan)
    {
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        var serverName = _cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = UnknownServer;
        }

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerBanDef(
            null,
            target,
            addressRange,
            hwid,
            DateTimeOffset.Now,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            serverName);

        await _db.AddServerBanAsync(banDef);
        var adminName = banningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = target is null ? "null" : $"{targetUsername} ({target})";
        var addressRangeString = addressRange != null
            ? $"{addressRange.Value.Item1}/{addressRange.Value.Item2}"
            : "null";
        var hwidString = hwid != null
            ? string.Concat(hwid.Value.Select(x => x.ToString("x2")))
            : "null";
        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", reason));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);


        if (banDef.UserId.HasValue)
        {
            var banlist = await _db.GetServerBansAsync(addressRange?.Item1, target, hwid, false);
            if (banlist.Count > 0 && banlist[^1].Id.HasValue)
            {
                var banDefWithId = new ServerBanDef(
                    banlist[^1].Id,
                    banDef.UserId,
                    banDef.Address,
                    banDef.HWId,
                    banDef.BanTime,
                    banDef.ExpirationTime,
                    banDef.RoundId,
                    banDef.PlaytimeAtNote,
                    banDef.Reason,
                    banDef.Severity,
                    banDef.BanningAdmin,
                    banDef.Unban,
                    banDef.ServerName);

                _cachedServerBans.GetOrNew(banDef.UserId.Value).Add(banDefWithId);
            }
        }

        // If we're not banning a player we don't care about disconnecting people
        if (target == null)
            return;

        // Is the player connected?
        if (!_playerManager.TryGetSessionById(target.Value, out var targetPlayer))
            return;
        // Kick when perma
        if (banDef.ExpirationTime == null)
        {
            var message = banDef.FormatBanMessage(_cfg, _localizationManager);
            targetPlayer.Channel.Disconnect(message);
        }
        else // Teleport to gulag
        {
            var gulag = _systems.GetEntitySystem<GulagSystem>();
            gulag.SendToGulag(targetPlayer);
        }
    }
    #endregion

    //Miracle edit start
    public HashSet<ServerBanDef> GetServerBans(NetUserId userId)
    {
        if (_cachedServerBans.TryGetValue(userId, out var bans))
        {
            return bans;
        }

        return new HashSet<ServerBanDef>();
    }

    public void RemoveCachedServerBan(NetUserId userId, int? id)
    {
        if (_cachedServerBans.TryGetValue(userId, out var bans))
        {
            bans.RemoveWhere(ban => ban.Id == id);
        }
    }

    public void AddCachedServerBan(ServerBanDef banDef)
    {
        if (banDef.UserId == null)
            return;

        _cachedServerBans.GetOrNew(banDef.UserId.Value).Add(banDef);
    }
    //Miracle edit end

    #region Job Bans
    // If you are trying to remove timeOfBan, please don't. It's there because the note system groups role bans by time, reason and banning admin.
    // Removing it will clutter the note list. Please also make sure that department bans are applied to roles with the same DateTimeOffset.
    public async void CreateRoleBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableArray<byte>? hwid, string role, uint? minutes, NoteSeverity severity, string reason, DateTimeOffset timeOfBan, bool isGlobalBan)
    {
        if (!_prototypeManager.TryIndex(role, out JobPrototype? _))
        {
            throw new ArgumentException($"Invalid role '{role}'", nameof(role));
        }

        role = string.Concat(JobPrefix, role);
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        var serverName = _cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = UnknownServer;
        }

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerRoleBanDef(
            null,
            target,
            addressRange,
            hwid,
            timeOfBan,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            role,
            serverName);

        if (!await AddRoleBan(banDef))
        {
            _chat.SendAdminAlert(Loc.GetString("cmd-roleban-existing", ("target", targetUsername ?? "null"), ("role", role)));
            return;
        }

        var length = expires == null ? Loc.GetString("cmd-roleban-inf") : Loc.GetString("cmd-roleban-until", ("expires", expires));
        _chat.SendAdminAlert(Loc.GetString("cmd-roleban-success", ("target", targetUsername ?? "null"), ("role", role), ("reason", reason), ("length", length)));

        if (target != null)
        {
            SendRoleBans(target.Value);
        }
    }

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetServerRoleBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddServerRoleUnbanAsync(new ServerRoleUnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        if (ban.UserId is { } player && _cachedRoleBans.TryGetValue(player, out var roleBans))
        {
            roleBans.RemoveWhere(roleBan => roleBan.Id == ban.Id);
            SendRoleBans(player);
        }

        return $"Pardoned ban with id {banId}";
    }

    public HashSet<string>? GetJobBans(NetUserId playerUserId)
    {
        if (!_cachedRoleBans.TryGetValue(playerUserId, out var roleBans))
            return null;
        return roleBans
            .Where(ban => ban.Role.StartsWith(JobPrefix, StringComparison.Ordinal))
            .Select(ban => ban.Role[JobPrefix.Length..])
            .ToHashSet();
    }
    #endregion

    public void SendRoleBans(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out var player))
        {
            return;
        }

        SendRoleBans(player);
    }

    public void SendRoleBans(ICommonSession pSession)
    {
        var roleBans = _cachedRoleBans.GetValueOrDefault(pSession.UserId) ?? new HashSet<ServerRoleBanDef>();
        var bans = new MsgRoleBans()
        {
            Bans = roleBans.Select(o => o.Role).ToList()
        };

        _sawmill.Debug($"Sent rolebans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }

    //WD start
    public async void UtkaCreateDepartmentBan(string admin, string target, DepartmentPrototype department, string reason, uint minutes, bool isGlobalBan,
        IPandaStatusHandlerContext context)
    {
        var located = await _playerLocator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddress = located.LastAddress;

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
        }

        (IPAddress, int)? addressRange = null;
        if (targetAddress != null)
        {
            if (targetAddress.IsIPv4MappedToIPv6)
                targetAddress = targetAddress.MapToIPv4();

            // Ban /64 for IPv4, /32 for IPv4.
            var cidr = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 64 : 32;
            addressRange = (targetAddress, cidr);
        }

        var cfg = UnsafePseudoIoC.ConfigurationManager;
        var serverName = cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = "unknown";
        }

        var locatedPlayer = await _playerLocator.LookupIdByNameOrIdAsync(admin);
        if (locatedPlayer == null)
        {
            UtkaSendResponse(false, context);
            return;
        }
        var player = locatedPlayer.UserId;

        UtkaSendResponse(true, context);

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = (await _db.GetPlayTimes(targetUid)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        foreach (var job in department.Roles)
        {
            var role = string.Concat(JobPrefix, job);

            var banDef = new ServerRoleBanDef(
                null,
                targetUid,
                addressRange,
                targetHWid,
                DateTimeOffset.Now,
                expires,
                roundId,
                playtime,
                reason,
                NoteSeverity.High,
                player,
                null,
                role,
                serverName);

            if (!await AddRoleBan(banDef))
                continue;

            var banId = await UtkaGetBanId(reason, role, targetUid);

            UtkaSendJobBanEvent(admin, target, minutes, job, isGlobalBan, reason, banId);
        }

        SendRoleBans(located);
    }

    public async void UtkaCreateJobBan(string admin, string target, string job, string reason, uint minutes, bool isGlobalBan,
        IPandaStatusHandlerContext context)
    {
        if (!_prototypeManager.TryIndex<JobPrototype>(job, out _))
        {
            UtkaSendResponse(false, context);
            return;
        }

        var role = string.Concat(JobPrefix, job);

        var located = await _playerLocator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddress = located.LastAddress;

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
        }

        (IPAddress, int)? addressRange = null;
        if (targetAddress != null)
        {
            if (targetAddress.IsIPv4MappedToIPv6)
                targetAddress = targetAddress.MapToIPv4();

            // Ban /64 for IPv4, /32 for IPv4.
            var cidr = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 64 : 32;
            addressRange = (targetAddress, cidr);
        }

        var cfg = UnsafePseudoIoC.ConfigurationManager;
        var serverName = cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = "unknown";
        }

        var locatedPlayer = await _playerLocator.LookupIdByNameOrIdAsync(admin);
        if (locatedPlayer == null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = (await _db.GetPlayTimes(targetUid)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var player = locatedPlayer.UserId;
        var banDef = new ServerRoleBanDef(
            null,
            targetUid,
            addressRange,
            targetHWid,
            DateTimeOffset.Now,
            expires,
            roundId,
            playtime,
            reason,
            NoteSeverity.High,
            player,
            null,
            role,
            serverName);

        if (!await AddRoleBan(banDef))
        {
            UtkaSendResponse(false, context);
            return;
        }

        var banId = await UtkaGetBanId(reason, role, targetUid);

        UtkaSendJobBanEvent(admin, target, minutes, job, isGlobalBan, reason, banId);
        UtkaSendResponse(true, context);

        SendRoleBans(located);
    }

    private void UtkaSendResponse(bool banned, IPandaStatusHandlerContext context)
    {
        var utkaBanned = new UtkaJobBanResponse()
        {
            Banned = banned
        };

        context.RespondJsonAsync(utkaBanned);
    }

    private async void UtkaSendJobBanEvent(string ackey, string ckey, uint duration, string job, bool global,
        string reason, int banId)
    {
        if (job.Contains("Job:"))
        {
            job = job.Replace("Job:", "");
        }

        var utkaBanned = new UtkaBannedEvent()
        {
            ACkey = ackey,
            Ckey = ckey,
            Duration = duration,
            Bantype = job,
            Global = global,
            Reason = reason,
            Rid = EntitySystem.Get<GameTicker>().RoundId,
            BanId = banId
        };

        _pandaWeb.SendBotPostMessage(utkaBanned);
        _entMan.EventBus.RaiseEvent(EventSource.Local, utkaBanned);
    }

    private async Task<int> UtkaGetBanId(string reason, string role, NetUserId targetUid)
    {
        var banId = 0;
        var banList = await _db.GetServerRoleBansAsync(null, targetUid, null);

        foreach (var ban in banList)
        {
            if (ban.Reason == reason)
            {
                if (ban.Role == role && ban.Id != null)
                {
                    banId = ban.Id.Value;
                }
            }
        }

        return banId;
    }

    public void SendRoleBans(LocatedPlayerData located)
    {
        if (!_playerManager.TryGetSessionById(located.UserId, out var player))
        {
            return;
        }

        SendRoleBans(player);
    }
    //WD end
}
