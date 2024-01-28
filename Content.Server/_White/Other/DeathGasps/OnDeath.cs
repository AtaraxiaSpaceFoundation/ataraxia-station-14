using Content.Server.Chat.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._White.Other.DeathGasps;

public sealed class OnDeath : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeathGaspsComponent, MobStateChangedEvent>(HandleDeathEvent);
        SubscribeLocalEvent<DeathGaspsComponent, PlayerDetachedEvent>(OnDetach);
    }

    private readonly Dictionary<EntityUid, EntityUid> _playingStreams = new();
    private static readonly SoundSpecifier DeathSounds = new SoundCollectionSpecifier("deathSounds");
    private static readonly SoundSpecifier HeartSounds = new SoundCollectionSpecifier("heartSounds");
    private static readonly string[] DeathGaspMessages =
    {
        "death-gasp-high",
        "death-gasp-medium",
        "death-gasp-normal"
    };

    private void HandleDeathEvent(EntityUid uid, DeathGaspsComponent component, MobStateChangedEvent args)
    {
        //^.^
        switch (args.NewMobState)
        {
            case MobState.Invalid:
                StopPlayingStream(uid);
                break;
            case MobState.Alive:
                StopPlayingStream(uid);
                break;
            case MobState.Critical:
                PlayPlayingStream(uid);
                break;
            case MobState.Dead:
                StopPlayingStream(uid);
                var deathGaspMessage = SelectRandomDeathGaspMessage();
                var localizedMessage = LocalizeDeathGaspMessage(deathGaspMessage);
                SendDeathGaspMessage(uid, localizedMessage);
                PlayDeathSound(uid);
                break;
        }
    }


    private void PlayPlayingStream(EntityUid uid)
    {
        if (_playingStreams.TryGetValue(uid, out var currentStream))
        {
            _audio.Stop(currentStream);
        }

        var newStream = _audio.PlayEntity(HeartSounds, uid, uid, AudioParams.Default.WithLoop(true));
        if (newStream != null)
            _playingStreams[uid] = newStream.Value.Entity;
    }

    private void StopPlayingStream(EntityUid uid)
    {
        if (_playingStreams.TryGetValue(uid, out var currentStream))
        {
            _audio.Stop(currentStream);
            _playingStreams.Remove(uid);
        }
    }

    private string SelectRandomDeathGaspMessage()
    {
        return DeathGaspMessages[_random.Next(DeathGaspMessages.Length)];
    }

    private string LocalizeDeathGaspMessage(string message)
    {
        return Loc.GetString(message);
    }

    private void SendDeathGaspMessage(EntityUid uid, string message)
    {
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Emote, ChatTransmitRange.Normal,
            ignoreActionBlocker: true);
    }

    private void PlayDeathSound(EntityUid uid)
    {
        _audio.PlayEntity(DeathSounds, uid, uid, AudioParams.Default);
    }

    private void OnDetach(EntityUid uid, DeathGaspsComponent component, PlayerDetachedEvent args)
    {
        StopPlayingStream(args.Entity);
    }
}
