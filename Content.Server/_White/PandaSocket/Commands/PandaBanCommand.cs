using System.Net;
using System.Net.Sockets;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Server._White.PandaSocket.Main;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server._White.PandaSocket.Commands;

public sealed class PandaBanCommand : IPandaCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly PandaWebManager _pandaWeb = default!;

    private const ILocalizationManager LocalizationManager = default!;

    public string Name => "ban";
    public Type RequestMessageType => typeof(UtkaBanRequest);
    public async void Execute(IPandaStatusHandlerContext context, PandaBaseMessage baseMessage)
    {
        if (baseMessage is not UtkaBanRequest message) return;

        var plyMgr = IoCManager.Resolve<IPlayerManager>();
        var locator = IoCManager.Resolve<IPlayerLocator>();
        IoCManager.InjectDependencies(this);

        var locatedPlayer = await locator.LookupIdByNameOrIdAsync(message.ACkey!);
        if (locatedPlayer == null)
        {
            UtkaSendResponse(context, false);
            return;
        }

        var player = locatedPlayer.UserId;

        var target = message.Ckey!;
        var reason = message.Reason!;
        var minutes = (uint) message.Duration!;
        var isGlobalBan = (bool) message.Global!;

        if (Enum.TryParse(message.Severity!, ignoreCase: true, out NoteSeverity severity))
        {
            UtkaSendResponse(context, false);
            return;
        }

        var located = await locator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            UtkaSendResponse(context, false);
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddr = located.LastAddress;

        if (player == targetUid)
        {
            UtkaSendResponse(context, false);
            return;
        }

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
        }

        (IPAddress, int)? addrRange = null;
        if (targetAddr != null)
        {
            if (targetAddr.IsIPv4MappedToIPv6)
                targetAddr = targetAddr.MapToIPv4();

            // Ban /64 for IPv4, /32 for IPv4.
            var cidr = targetAddr.AddressFamily == AddressFamily.InterNetworkV6 ? 64 : 32;
            addrRange = (targetAddr, cidr);
        }

        var serverName = _cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = "unknown";
        }

        IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = (await _db.GetPlayTimes(targetUid)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerBanDef(
            null,
            targetUid,
            addrRange,
            targetHWid,
            DateTimeOffset.Now,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            player,
            null,
            serverName);

        UtkaSendResponse(context, true);

        await _db.AddServerBanAsync(banDef);

        if (plyMgr.TryGetSessionById(targetUid, out var targetPlayer))
        {
            var msg = banDef.FormatBanMessage(_cfg, LocalizationManager);
            targetPlayer.ConnectedClient.Disconnect(msg);
        }

        var banlist = await _db.GetServerBansAsync(null, targetUid, null);
        var banId = banlist[^1].Id;

        var utkaBanned = new UtkaBannedEvent()
        {
            Ckey = message.Ckey,
            ACkey = message.ACkey,
            Bantype = "server",
            Duration = message.Duration,
            Global = message.Global,
            Reason = message.Reason,
            Rid = EntitySystem.Get<GameTicker>().RoundId,
            BanId = banId
        };

        _pandaWeb.SendBotPostMessage(utkaBanned);
        _entMan.EventBus.RaiseEvent(EventSource.Local, utkaBanned);
    }

    public void Response(IPandaStatusHandlerContext context, PandaBaseMessage? message = null)
    {
        context.RespondJsonAsync(message!);
    }

    private void UtkaSendResponse(IPandaStatusHandlerContext context, bool banned)
    {
        var utkaResponse = new UtkaBanResponse()
        {
            Banned = banned
        };

        Response(context, utkaResponse);
    }
}
