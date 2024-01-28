using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server._White.AspectsSystem.Base;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Server._White.PandaSocket.Main;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared._White;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server._White.Reputation;

public sealed class ReputationSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ReputationManager _repManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    private const int MinPlayers = 15;
    private const int MinRoundLength = 25;
    private const int MinTimePlayerConnected = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UtkaBannedEvent>(ModifyReputationOnPlayerBanned);
    }

    /// <summary>
    /// Tries to modify reputation on round end and then returns it's new value and delta value if successful.
    /// </summary>
    /// <param name="name">Player to get new values for.</param>
    /// <param name="newValue">Modified player's reputation value.</param>
    /// <param name="deltaValue"></param>
    /// <returns>Success in modifying player's reputation.</returns>
    public bool TryModifyReputationOnRoundEnd(string name, out float? newValue, out float? deltaValue)
    {
        newValue = null;
        deltaValue = null;

        var repEnabled = _cfg.GetCVar(WhiteCVars.ReputationEnabled);
        if (!repEnabled)
            return false;

        if (!_playerManager.TryGetSessionByUsername(name, out var session) || session.AttachedEntity == null)
            return false;

        if (!TryCalculatePlayerReputation(session.AttachedEntity.Value, out var delta))
            return false;

        var uid = session.UserId;
        _repManager.GetCachedPlayerReputation(uid, out var value);

        if (value == null)
            return false;

        var longConnected = _repManager.GetCachedPlayerConnection(uid, out var date)
                             && DateTime.UtcNow - date >= TimeSpan.FromMinutes(MinTimePlayerConnected);
        var longRound = _gameTicker.RoundDuration() >= TimeSpan.FromMinutes(MinRoundLength);
        var enoughPlayers = _playerManager.PlayerCount >= MinPlayers;

        if (delta != 0 && longRound && longConnected && enoughPlayers)
        {
            _repManager.ModifyPlayerReputation(uid, delta);
        }

        deltaValue = longRound && longConnected && enoughPlayers ? delta : 0f;
        newValue = value + deltaValue;

        return true;
    }

    private bool TryCalculatePlayerReputation(EntityUid entity, out float deltaValue)
    {
        deltaValue = 0f;
        var aspect = false;

        if (!TryComp<MobStateComponent>(entity, out var state) || state.CurrentState is MobState.Dead or MobState.Invalid)
            return true;

        var ruleEnt = _gameTicker.GetActiveGameRules()
            .Where(HasComp<AspectComponent>)
            .FirstOrNull();

        if (ruleEnt != null)
        {
            if (TryComp<AspectComponent>(ruleEnt, out var comp))
            {
                deltaValue += comp.Weight switch
                {
                    3 => 2f,
                    2 => 3f,
                    1 => 4f,
                    _ => 0f
                };
                aspect = true;
            }
        }

        if (!aspect)
            deltaValue += 1f;

        if (TryComp<MindContainerComponent>(entity, out var mind)
            && mind.Mind != null
            && _roles.MindIsAntagonist(mind.Mind)
            && TryComp(mind.Mind, out MindComponent? mindComp))
        {
            var objCompleted = 0;
            var totalObj = 0;
            foreach (var obj in mindComp.Objectives)
            {
                totalObj++;

                var info = _objectives.GetInfo(obj, mind.Mind.Value, mindComp);

                if (info is {Progress: > 0.99f})
                    objCompleted++;
            }

            if (aspect)
            {
                if (objCompleted == totalObj)
                    deltaValue += 1f + objCompleted;
                else
                    deltaValue += 1f + objCompleted * 0.5f;
            }
            else
            {
                if (objCompleted == totalObj)
                    deltaValue += 2f + objCompleted * 0.5f;
                else
                    deltaValue += objCompleted * 0.5f;
            }
        }

        return true;
    }

    private async void ModifyReputationOnPlayerBanned(UtkaBannedEvent ev)
    {
        NetUserId uid;
        float value;

        if (ev.Bantype == "server")
        {
            value = ev.Duration switch
            {
                > 10080 => -10f,
                > 4320 => -7f,
                > 1440 => -5f,
                0 => -25f,
                _ => -3f
            };
        }
        else
            value = -2f;

        if (_playerManager.TryGetPlayerDataByUsername(ev.Ckey!, out var data))
            uid = data.UserId;
        else
        {
            var located = await _locator.LookupIdByNameAsync(ev.Ckey!);

            if (located == null)
                return;

            uid = located.UserId;
        }

        _repManager.ModifyPlayerReputation(uid, value);
    }
}
