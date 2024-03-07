using Content.Shared._White.Wizard;
using Content.Shared._White.Wizard.Charging;
using Content.Shared.Follower;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._White.Wizard.Charging;

public sealed class ChargingSystem : SharedChargingSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;

    private readonly Dictionary<EntityUid, List<EntityUid>> _charges = new();

    private readonly Dictionary<EntityUid, EntityUid> _chargingLoops = new();
    private readonly Dictionary<EntityUid, EntityUid> _chargedLoop = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestSpellChargingAudio>(OnCharging);
        SubscribeNetworkEvent<RequestSpellChargedAudio>(OnCharged);
        SubscribeNetworkEvent<RequestAudioSpellStop>(OnStop);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnDetach);

        SubscribeNetworkEvent<AddWizardChargeEvent>(Add);
        SubscribeNetworkEvent<RemoveWizardChargeEvent>(Remove);
    }

    #region Audio

    private void OnCharging(RequestSpellChargingAudio msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession?.AttachedEntity;
        if (user == null)
            return;

        var shouldLoop = msg.Loop;
        var sound = msg.Sound;

        if (!shouldLoop)
        {
            _audio.PlayPvs(sound, user.Value);
            return;
        }

        if (_chargingLoops.TryGetValue(user.Value, out var currentStream))
        {
            _audio.Stop(currentStream);
            _chargingLoops.Remove(user.Value);
        }

        var newStream = _audio.PlayPvs(sound, user.Value, AudioParams.Default.WithLoop(true));

        if (newStream.HasValue)
        {
            _chargingLoops[user.Value] = newStream.Value.Entity;
        }
    }

    private void OnCharged(RequestSpellChargedAudio msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession?.AttachedEntity;
        if (user == null)
            return;

        if (_chargingLoops.TryGetValue(user.Value, out var currentStream))
        {
            _audio.Stop(currentStream);
            _chargingLoops.Remove(user.Value);
        }

        var shouldLoop = msg.Loop;
        var sound = msg.Sound;

        if (!shouldLoop)
        {
            _audio.PlayPvs(sound, user.Value);
            return;
        }

        if (_chargedLoop.TryGetValue(user.Value, out var chargedLoop))
        {
            _audio.Stop(chargedLoop);
            _chargedLoop.Remove(user.Value);
        }

        var newStream = _audio.PlayPvs(sound, user.Value, AudioParams.Default.WithLoop(true));

        if (newStream.HasValue)
        {
            _chargedLoop[user.Value] = newStream.Value.Entity;
        }
    }

    private void OnStop(RequestAudioSpellStop msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession?.AttachedEntity;
        if (user == null)
            return;

        if (_chargingLoops.TryGetValue(user.Value, out var currentStream))
        {
            _audio.Stop(currentStream);
            _chargingLoops.Remove(user.Value);
        }

        if (_chargedLoop.TryGetValue(user.Value, out var chargedLoop))
        {
            _audio.Stop(chargedLoop);
            _chargedLoop.Remove(user.Value);
        }
    }

    private void OnDetach(PlayerDetachedEvent msg, EntitySessionEventArgs args)
    {
        var user = msg.Entity;

        if (_chargingLoops.TryGetValue(user, out var currentStream))
        {
            _audio.Stop(currentStream);
            _chargingLoops.Remove(user);
        }

        if (_chargedLoop.TryGetValue(user, out var chargedLoop))
        {
            _audio.Stop(chargedLoop);
            _chargedLoop.Remove(user);
        }
    }

    #endregion

    #region Charges

    private void Add(AddWizardChargeEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            AddCharge(args.SenderSession.AttachedEntity.Value, msg.ChargeProto);
    }

    private void Remove(RemoveWizardChargeEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            RemoveAllCharges(args.SenderSession.AttachedEntity.Value);
    }

    #endregion

    #region Helpers

    public void AddCharge(EntityUid uid, string msgChargeProto)
    {
        var itemEnt = Spawn(msgChargeProto, Transform(uid).Coordinates);
        _followerSystem.StartFollowingEntity(itemEnt, uid);

        if (!_charges.ContainsKey(uid))
        {
            _charges[uid] = new List<EntityUid>();
        }

        _charges[uid].Add(itemEnt);
    }

    public void RemoveAllCharges(EntityUid uid)
    {
        if (!_charges.ContainsKey(uid))
            return;

        foreach (var followerEnt in _charges[uid])
        {
            Del(followerEnt);
        }

        _charges.Remove(uid);
    }

    #endregion
}
