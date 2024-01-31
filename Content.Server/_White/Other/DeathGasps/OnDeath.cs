using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._White.Other.DeathGasps;

public sealed class OnDeath : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeathGaspsComponent, MobStateChangedEvent>(HandleDeathEvent);
        SubscribeLocalEvent<DeathGaspsComponent, PlayerDetachedEvent>(OnDetach);
    }

    private readonly Dictionary<EntityUid, EntityUid> _playingStreams = new();
    private static readonly SoundSpecifier DeathSounds = new SoundCollectionSpecifier("deathSounds");
    private static readonly SoundSpecifier HeartSounds = new SoundCollectionSpecifier("heartSounds");

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

        if (newStream.HasValue)
        {
            _playingStreams[uid] = newStream.Value.Entity;
        }
    }

    private void StopPlayingStream(EntityUid uid)
    {
        if (!_playingStreams.TryGetValue(uid, out var currentStream))
            return;

        _audio.Stop(currentStream);
        _playingStreams.Remove(uid);
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
