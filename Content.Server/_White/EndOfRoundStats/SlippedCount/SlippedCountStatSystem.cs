using System.Linq;
using System.Text;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Slippery;
using Content.Shared._White;
using Robust.Shared.Configuration;

namespace Content.Server._White.EndOfRoundStats.SlippedCount;

public sealed class SlippedCountStatSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private readonly Dictionary<PlayerData, int> _userSlipStats = new();

    private struct PlayerData
    {
        public string Name;
        public string? Username;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipperyComponent, SlipEvent>(OnSlip);

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnSlip(EntityUid uid, SlipperyComponent slipComp, ref SlipEvent args)
    {
        string? username = null;

        var entity = args.Slipped;

        if (EntityManager.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            username = mindComp.CharacterName;
        }

        var playerData = new PlayerData
        {
            Name = MetaData(entity).EntityName,
            Username = username
        };

        if (!_userSlipStats.TryAdd(playerData, 1))
        {
            _userSlipStats[playerData]++;
        }
    }

    private void OnRoundEnd(RoundEndTextAppendEvent ev)
    {
        if (_config.GetCVar(WhiteCVars.SlippedCountTopSlipper) == false)
            return;

        var sortedSlippers = _userSlipStats.OrderByDescending(m => m.Value).ToList();

        var totalTimesSlipped = sortedSlippers.Sum(m => m.Value);

        var sb = new StringBuilder("\n[color=springGreen]");

        if (totalTimesSlipped < _config.GetCVar(WhiteCVars.SlippedCountThreshold))
        {
            if (totalTimesSlipped == 0 && _config.GetCVar(WhiteCVars.SlippedCountDisplayNone))
            {
                sb.Append(Loc.GetString("eorstats-slippedcount-none"));
            }
            else
                return;
        }
        else
        {
            sb.AppendLine(Loc.GetString("eorstats-slippedcount-totalslips", ("timesSlipped", totalTimesSlipped)));
            sb.Append(GenerateTopSlipper(sortedSlippers.First().Key, sortedSlippers.First().Value));
        }

        sb.Append("[/color]");
        ev.AddLine(sb.ToString());
    }

    private string GenerateTopSlipper(PlayerData data, int amountSlipped)
    {
        if (data.Username != null)
        {
            return Loc.GetString
            (
                "eorstats-slippedcount-topslipper-hasusername",
                ("username", data.Username),
                ("name", data.Name),
                ("slipcount", amountSlipped)
            );
        }

        return Loc.GetString
        (
            "eorstats-slippedcount-topslipper-hasnousername",
            ("name", data.Name),
            ("slipcount", amountSlipped)
        );
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _userSlipStats.Clear();
    }
}
