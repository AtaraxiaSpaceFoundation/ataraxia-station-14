using System.Text;
using Content.Server.GameTicking;
using Content.Shared.Cuffs.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared._White;
using Content.Shared._White.EndOfRoundStats.CuffedTime;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._White.EndOfRoundStats.CuffedTime;

public sealed class CuffedTimeStatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private readonly Dictionary<PlayerData, TimeSpan> _userPlayStats = new();

    private struct PlayerData
    {
        public string Name;
        public string? Username;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffableComponent, CuffedTimeStatEvent>(OnUncuffed);

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnUncuffed(EntityUid uid, CuffableComponent component, CuffedTimeStatEvent args)
    {
        string? username = null;

        if (EntityManager.TryGetComponent<MindComponent>(uid, out var mindComponent) &&
            mindComponent.Session != null)
            username = mindComponent.Session.Name;

        var playerData = new PlayerData
        {
            Name = MetaData(uid).EntityName,
            Username = username
        };

        if (_userPlayStats.ContainsKey(playerData))
        {
            _userPlayStats[playerData] += args.Duration;
            return;
        }

        _userPlayStats.Add(playerData, args.Duration);
    }

    private void OnRoundEnd(RoundEndTextAppendEvent ev)
    {
        // Gather any people currently cuffed.
        // Otherwise people cuffed on the evac shuttle will not be counted.
        var query = EntityQueryEnumerator<CuffableComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CuffedTime != null)
                RaiseLocalEvent(uid, new CuffedTimeStatEvent(_gameTiming.CurTime - component.CuffedTime.Value));
        }

        // Continue with normal logic.
        var sb = new StringBuilder("\n[color=cadetblue]");

        (PlayerData Player, TimeSpan TimePlayed) topPlayer = (new PlayerData(), TimeSpan.Zero);

        foreach (var (player, timePlayed) in _userPlayStats)
        {
            if (timePlayed >= topPlayer.TimePlayed)
                topPlayer = (player, timePlayed);
        }

        if (topPlayer.TimePlayed < TimeSpan.FromMinutes(_config.GetCVar(WhiteCVars.CuffedTimeThreshold)))
            return;

        sb.Append(GenerateTopPlayer(topPlayer.Item1, topPlayer.Item2));
        sb.Append("[/color]");
        ev.AddLine(sb.ToString());
    }

    private string GenerateTopPlayer(PlayerData data, TimeSpan timeCuffed)
    {
        var line = String.Empty;

        if (data.Username != null)
            line += Loc.GetString
            (
                "eorstats-cuffedtime-hasusername",
                ("username", data.Username),
                ("name", data.Name),
                ("timeCuffedMinutes", Math.Round(timeCuffed.TotalMinutes))
            );
        else
            line += Loc.GetString
            (
                "eorstats-cuffedtime-hasnousername",
                ("name", data.Name),
                ("timeCuffedMinutes", Math.Round(timeCuffed.TotalMinutes))
            );

        return line;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _userPlayStats.Clear();
    }
}
