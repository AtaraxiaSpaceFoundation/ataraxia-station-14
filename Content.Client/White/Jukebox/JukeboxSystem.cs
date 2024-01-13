using System.Resources;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.White;
using Content.Shared.White.Jukebox;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Client.White.Jukebox;

public sealed class JukeboxSystem : EntitySystem
{

    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;




    private readonly Dictionary<JukeboxComponent, JukeboxAudio> _playingJukeboxes = new();

    private float _maxAudioRange;
    private float _jukeboxVolume;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(WhiteCVars.MaxJukeboxSoundRange, range => _maxAudioRange = range, true);
        _cfg.OnValueChanged(WhiteCVars.JukeboxVolume, volume => JukeboxVolumeChanged(volume), true);

        SubscribeLocalEvent<JukeboxComponent, ComponentHandleState>(OnStateChanged);
        SubscribeLocalEvent<JukeboxComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeNetworkEvent<TickerJoinLobbyEvent>(JoinLobby);
        SubscribeNetworkEvent<JukeboxStopPlaying>(OnStopPlaying);
    }

    private void JukeboxVolumeChanged(float volume)
    {
        _jukeboxVolume = volume;

        CleanUp();
    }

    private void JoinLobby(TickerJoinLobbyEvent ev)
    {
        CleanUp();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanUp();
    }

    private void OnComponentRemoved(EntityUid uid, JukeboxComponent component, ComponentRemove args)
    {
        if (!_playingJukeboxes.TryGetValue(component, out var playingData)) return;
        _audioSystem.Stop(playingData.PlayingStream, playingData.Component);
        _playingJukeboxes.Remove(component);
    }

    private void OnStopPlaying(JukeboxStopPlaying ev)
    {
        if (!ev.JukeboxUid.HasValue) return;
        if(!TryComp<JukeboxComponent>(GetEntity(ev.JukeboxUid), out var jukeboxComponent)) return;

        if(!_playingJukeboxes.TryGetValue(jukeboxComponent, out var jukeboxAudio)) return;

        _audioSystem.Stop(jukeboxAudio.PlayingStream, jukeboxAudio.Component);
        _playingJukeboxes.Remove(jukeboxComponent);
    }

    public void RequestSongToPlay(JukeboxComponent component, JukeboxSong jukeboxSong)
    {
        if (!_resource.TryGetResource<AudioResource>((ResPath) jukeboxSong.SongPath!, out var songResource))
        {
            return;
        }

        RaiseNetworkEvent(new JukeboxRequestSongPlay()
        {
            Jukebox = GetNetEntity(component.Owner),
            SongName = jukeboxSong.SongName,
            SongPath = jukeboxSong.SongPath,
            SongDuration = (float)songResource.AudioStream.Length.TotalSeconds
        });

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var localPlayerEntity = _playerManager.LocalPlayer!.ControlledEntity;
        if(!localPlayerEntity.HasValue) return;

        ProcessJukeboxes();
    }

    private void OnStateChanged(EntityUid uid, JukeboxComponent component, ref ComponentHandleState args)
    {
        if (args.Current is JukeboxComponentState state)
        {
            component.Repeating = state.Playing;
            component.Volume = state.Volume;
            component.PlayingSongData = state.SongData;
        }
    }

    private void ProcessJukeboxes()
    {
        var jukeboxes = EntityQuery<JukeboxComponent, TransformComponent>();
        var playerXform = Comp<TransformComponent>(_playerManager.LocalPlayer!.ControlledEntity!.Value);

        foreach (var (jukeboxComponent, jukeboxXform) in jukeboxes)
        {

            if(jukeboxXform.MapID != playerXform.MapID) continue;
            if ((jukeboxXform.MapPosition.Position - playerXform.MapPosition.Position).Length() > _maxAudioRange) continue;

            if (_playingJukeboxes.TryGetValue(jukeboxComponent, out var jukeboxAudio))
            {
                if (!jukeboxAudio.Component.Playing)
                {
                    HandleDoneStream(jukeboxAudio, jukeboxComponent);
                    return;
                }

                if (jukeboxAudio.SongData.SongPath != jukeboxComponent.PlayingSongData?.SongPath)
                {
                    HandleSongChanged(jukeboxAudio, jukeboxComponent);
                    return;
                }
            }
            else
            {
                if (jukeboxComponent.PlayingSongData == null)
                {
                    SetBarsLayerVisible(jukeboxComponent, false);
                    continue;
                }

                var stream = TryCreateStream(jukeboxComponent);

                if (stream == null)
                {
                    return;
                }

                _playingJukeboxes.Add(jukeboxComponent, stream);
                SetBarsLayerVisible(jukeboxComponent, true);
            }
        }
    }

    private void HandleSongChanged(JukeboxAudio jukeboxAudio, JukeboxComponent jukeboxComponent)
    {
        _audioSystem.Stop(jukeboxAudio.PlayingStream, jukeboxAudio.Component);

        if (jukeboxComponent.PlayingSongData != null && jukeboxComponent.PlayingSongData.SongPath == jukeboxAudio.SongData.SongPath)
        {
            var newStream = TryCreateStream(jukeboxComponent);
            if(newStream == null) return;

            _playingJukeboxes[jukeboxComponent] = newStream;
            SetBarsLayerVisible(jukeboxComponent, true);
        }
        else
        {
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukeboxComponent, false);
        }
    }

    private void HandleDoneStream(JukeboxAudio jukeboxAudio, JukeboxComponent jukeboxComponent)
    {
        if (!jukeboxComponent.Repeating)
        {
            _audioSystem.Stop(jukeboxAudio.PlayingStream, jukeboxAudio.Component);
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukeboxComponent, false);
            return;
        }

        if(jukeboxComponent.PlayingSongData == null) return;


        var newStream = TryCreateStream(jukeboxComponent);

        if (newStream == null)
        {
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukeboxComponent, false);
        }
        else
        {

            _playingJukeboxes[jukeboxComponent] = newStream;
            SetBarsLayerVisible(jukeboxComponent, true);
        }
    }

    private JukeboxAudio? TryCreateStream(JukeboxComponent jukeboxComponent)
    {
        if (jukeboxComponent.PlayingSongData == null) return null;

        var resourcePath = jukeboxComponent.PlayingSongData.SongPath!;
        var localSession = _playerManager.LocalPlayer!.Session;

        if(!_resource.TryGetResource<AudioResource>((ResPath) resourcePath, out var audio))
            return null!;

        if (audio!.AudioStream.Length.TotalSeconds < jukeboxComponent.PlayingSongData!.PlaybackPosition)
        {
            return null!;
        }

        var audioParams = new AudioParams
        {
            PlayOffsetSeconds = jukeboxComponent.PlayingSongData.PlaybackPosition,
            Volume = _jukeboxVolume,
            MaxDistance = _maxAudioRange
        };

        var playingStream = _audioSystem.PlayEntity(resourcePath.ToString()!, localSession, jukeboxComponent.Owner, audioParams);

        return new JukeboxAudio(playingStream.Value.Entity, playingStream.Value.Component, audio!, jukeboxComponent.PlayingSongData);
    }

    private class JukeboxAudio
    {
        public PlayingSongData SongData { get; }
        public EntityUid PlayingStream { get; }
        public AudioComponent Component { get; }
        public AudioResource AudioStream { get; }

        public JukeboxAudio(EntityUid playingStream, AudioComponent component, AudioResource audioStream, PlayingSongData songData)
        {
            PlayingStream = playingStream;
            Component = component;
            AudioStream = audioStream;
            SongData = songData;
        }
    }

    private void SetBarsLayerVisible(JukeboxComponent jukeboxComponent, bool visible)
    {
        var spriteComponent = Comp<SpriteComponent>(jukeboxComponent.Owner);
        spriteComponent.LayerMapTryGet("bars", out var layer);
        spriteComponent.LayerSetVisible(layer, visible);
    }

    private void CleanUp()
    {
        foreach (var playingJukebox in _playingJukeboxes.Values)
        {
            _audioSystem.Stop(playingJukebox.PlayingStream, playingJukebox.Component);
        }

        _playingJukeboxes.Clear();
    }
}
