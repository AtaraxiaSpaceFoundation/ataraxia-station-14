using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Shared.Physics;
using Content.Shared._White;
using Content.Shared._White.TTS;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Client._White.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IAudioManager _audioSystem = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedPhysicsSystem _broadPhase = default!;

    private ISawmill _sawmill = default!;
    private float _volume = 0.0f;

    private readonly HashSet<AudioStream> _currentStreams = new();
    private readonly Dictionary<EntityUid, Queue<AudioStream>> _entityQueues = new();

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(WhiteCVars.TtsVolume, OnTtsVolumeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(WhiteCVars.TtsVolume, OnTtsVolumeChanged);
        EndStreams();
    }

    // Little bit of duplication logic from AudioSystem
    public override void FrameUpdate(float frameTime)
    {
        var streamToRemove = new HashSet<AudioStream>();

        var ourPos = _eye.CurrentEye.Position.Position;
        foreach (var stream in _currentStreams)
        {
            if (!stream.Source.Playing ||
                !_entity.TryGetComponent<MetaDataComponent>(stream.Uid, out var meta) ||
                Deleted(stream.Uid, meta) ||
                !_entity.TryGetComponent<TransformComponent>(stream.Uid, out var xform))
            {
                stream.Source.Dispose();
                streamToRemove.Add(stream);
                continue;
            }

            var mapPos = xform.MapPosition;
            if (mapPos.MapId != MapId.Nullspace)
            {
                stream.Source.Position = mapPos.Position;
            }

            if (mapPos.MapId == _eye.CurrentMap)
            {
                var collisionMask = (int) CollisionGroup.Impassable;
                var sourceRelative = ourPos - mapPos.Position;
                var occlusion = 0f;
                if (sourceRelative.Length() > 0)
                {
                    occlusion = _broadPhase.IntersectRayPenetration(mapPos.MapId,
                        new CollisionRay(mapPos.Position, sourceRelative.Normalized(), collisionMask),
                        sourceRelative.Length(), stream.Uid);
                }

                stream.Source.Occlusion = occlusion;
            }
        }

        foreach (var audioStream in streamToRemove)
        {
            _currentStreams.Remove(audioStream);
            ProcessEntityQueue(audioStream.Uid);
        }
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        if (_volume <= -20f)
            return;

        var volume = _volume;
        if (ev.BoostVolume)
            volume += 5f;
        if (!TryCreateAudioSource(ev.Data, out var source, volume))
            return;

        var stream = new AudioStream(GetEntity(ev.Uid), source);
        AddEntityStreamToQueue(stream);
    }

    public void PlayCustomText(string text)
    {
        RaiseNetworkEvent(new RequestTTSEvent(text));
    }

    private bool TryCreateAudioSource(byte[] data, [NotNullWhen(true)] out IAudioSource? source, float volume = 0f)
    {
        var dataStream = new MemoryStream(data) { Position = 0 };
        var audioStream = _audioSystem.LoadAudioOggVorbis(dataStream);
        source = _audioSystem.CreateAudioSource(audioStream);
        if (source == null)
        {
            return false;
        }

        source.Volume = volume == 0f ? _volume : volume;
        return true;
    }

    private void AddEntityStreamToQueue(AudioStream stream)
    {
        if (_entityQueues.TryGetValue(stream.Uid, out var queue))
        {
            queue.Enqueue(stream);
        }
        else
        {
            _entityQueues.Add(stream.Uid, new Queue<AudioStream>(new[] { stream }));

            if (!IsEntityCurrentlyPlayStream(stream.Uid))
                ProcessEntityQueue(stream.Uid);
        }
    }

    private bool IsEntityCurrentlyPlayStream(EntityUid uid)
    {
        return _currentStreams.Any(s => s.Uid == uid);
    }

    private void ProcessEntityQueue(EntityUid uid)
    {
        if (TryTakeEntityStreamFromQueue(uid, out var stream))
            PlayEntity(stream);
    }

    private bool TryTakeEntityStreamFromQueue(EntityUid uid, [NotNullWhen(true)] out AudioStream? stream)
    {
        if (_entityQueues.TryGetValue(uid, out var queue))
        {
            stream = queue.Dequeue();
            if (queue.Count == 0)
                _entityQueues.Remove(uid);

            return true;
        }

        stream = null;
        return false;
    }

    private void PlayEntity(AudioStream stream)
    {
        if (!_entity.TryGetComponent<TransformComponent>(stream.Uid, out var xform))
            return;

        stream.Source.Position = xform.WorldPosition;
        stream.Source.StartPlaying();
        _currentStreams.Add(stream);
    }

    public void StopAllStreams()
    {
        foreach (var stream in _currentStreams)
        {
            stream.Source.StopPlaying();
        }
    }

    private void EndStreams()
    {
        foreach (var stream in _currentStreams)
        {
            stream.Source.StopPlaying();
            stream.Source.Dispose();
        }

        _currentStreams.Clear();
        _entityQueues.Clear();
    }

    // ReSharper disable once InconsistentNaming
    private sealed class AudioStream
    {
        public EntityUid Uid { get; }

        public IAudioSource Source { get; }

        public AudioStream(EntityUid uid, IAudioSource source)
        {
            Uid = uid;
            Source = source;
        }
    }
}
