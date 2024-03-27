using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared._White.Jukebox;
using Robust.Server.GameStates;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server._White.Jukebox;

public sealed class JukeboxSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverrideSystem = default!;

    private readonly List<JukeboxComponent> _playingJukeboxes = new() { };

    private const float UpdateTimerDefaultTime = 1f;
    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<JukeboxRequestSongPlay>(OnSongRequestPlay);
        SubscribeLocalEvent<JukeboxComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopRequest>(OnRequestStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxRepeatToggled>(OnRepeatToggled);
        SubscribeLocalEvent<JukeboxComponent, JukeboxEjectRequest>(OnEjectRequest);
        SubscribeLocalEvent<JukeboxComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        _pvsOverrideSystem.AddGlobalOverride(uid);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _playingJukeboxes.Clear();
    }

    private void OnEjectRequest(EntityUid uid, JukeboxComponent component, JukeboxEjectRequest args)
    {
        if (component.PlayingSongData != null) return;

        var containedEntities = component.TapeContainer.ContainedEntities;

        if (containedEntities.Count > 0)
        {
            _containerSystem.EmptyContainer(component.TapeContainer, true);
        }
    }

    private void OnGetVerb(EntityUid uid, JukeboxComponent jukeboxComponent, GetVerbsEvent<Verb> ev)
    {
        if (ev.Hands == null) return;
        if (jukeboxComponent.PlayingSongData != null) return;
        if (jukeboxComponent.TapeContainer.ContainedEntities.Count == 0) return;

        var removeTapeVerb = new Verb
        {
            Text = "Вытащить касету",
            Priority = 10000,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/remove_tape.png")),
            Act = () =>
            {
                var tapes = jukeboxComponent.TapeContainer.ContainedEntities.ToList();
                _containerSystem.EmptyContainer(jukeboxComponent.TapeContainer, true);

                foreach (var tape in tapes)
                {
                    _handsSystem.PickupOrDrop(ev.User, tape);
                }
            }
        };

        ev.Verbs.Add(removeTapeVerb);
    }

    private void OnRepeatToggled(EntityUid uid, JukeboxComponent component, JukeboxRepeatToggled args)
    {
        component.Playing = args.NewState;
        Dirty(uid, component);
    }

    private void OnRequestStop(EntityUid uid, JukeboxComponent component, JukeboxStopRequest args)
    {
        component.PlayingSongData = null;
        Dirty(uid, component);
    }

    private void OnInteract(EntityUid uid, JukeboxComponent component, InteractUsingEvent args)
    {
        if (component.PlayingSongData != null) return;

        if (!HasComp<TapeComponent>(args.Used))
            return;

        var containedEntities = component.TapeContainer.ContainedEntities;

        if (containedEntities.Count >= 1)
        {
            var removedTapes = _containerSystem.EmptyContainer(component.TapeContainer, true).ToList();
            _containerSystem.Insert(args.Used, component.TapeContainer);

            foreach (var tapeUid in removedTapes)
            {
                _handsSystem.PickupOrDrop(args.User, tapeUid);
            }
        }
        else
        {
            _containerSystem.Insert(args.Used, component.TapeContainer);
        }
    }

    private void OnSongRequestPlay(JukeboxRequestSongPlay msg, EntitySessionEventArgs args)
    {
        var entity = GetEntity(msg.Jukebox!.Value);
        var jukebox = Comp<JukeboxComponent>(entity);
        jukebox.Playing = true;

        var songData = new PlayingSongData
        {
            SongName = msg.SongName,
            SongPath = msg.SongPath,
            ActualSongLengthSeconds = msg.SongDuration,
            PlaybackPosition = 0f
        };

        jukebox.PlayingSongData = songData;

        _playingJukeboxes.Add(jukebox);

        Dirty(entity, jukebox);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_updateTimer <= UpdateTimerDefaultTime)
        {
            _updateTimer += frameTime;
            return;
        }

        ProcessPlayingJukeboxes();
    }

    private void ProcessPlayingJukeboxes()
    {
        for (var i = _playingJukeboxes.Count - 1; i >= 0; i--)
        {
            var playingJukeboxData = _playingJukeboxes[i];

            if (playingJukeboxData.PlayingSongData == null)
            {
                _playingJukeboxes.RemoveAt(i);
                continue;
            }

            playingJukeboxData.PlayingSongData.PlaybackPosition += _updateTimer;

            if (playingJukeboxData.PlayingSongData.PlaybackPosition >=
                playingJukeboxData.PlayingSongData.ActualSongLengthSeconds)
            {
                if (playingJukeboxData.Playing)
                {
                    playingJukeboxData.PlayingSongData.PlaybackPosition = 0;
                }
                else
                {
                    RaiseNetworkEvent(new JukeboxStopPlaying());
                    _playingJukeboxes.RemoveAt(i);
                }
            }

            Dirty(playingJukeboxData);
        }

        _updateTimer = 0;
    }
}