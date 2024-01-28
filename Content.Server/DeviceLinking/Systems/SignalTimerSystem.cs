using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.Radio.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.MachineLinking;
using Content.Shared.Radio;
using Content.Shared.TextScreen;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SignalTimerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <summary>
    /// Per-tick timer cache.
    /// </summary>
    private readonly List<Entity<SignalTimerComponent>> _timers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignalTimerComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);

        SubscribeLocalEvent<SignalTimerComponent, SignalTimerTextChangedMessage>(OnTextChangedMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalTimerDelayChangedMessage>(OnDelayChangedMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalTimerStartMessage>(OnTimerStartMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
    {
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Label);
        _signalSystem.EnsureSinkPorts(uid, component.Trigger);
    }

    private void OnAfterActivatableUIOpen(
        EntityUid uid,
        SignalTimerComponent component,
        AfterActivatableUIOpenEvent args)
    {
        var time = TryComp<ActiveSignalTimerComponent>(uid, out var active) ? active.TriggerTime : TimeSpan.Zero;

        if (_ui.TryGetUi(uid, SignalTimerUiKey.Key, out var bui))
        {
            _ui.SetUiState(bui, new SignalTimerBoundUserInterfaceState(component.Label,
                TimeSpan.FromSeconds(component.Delay).Minutes.ToString("D2"),
                TimeSpan.FromSeconds(component.Delay).Seconds.ToString("D2"),
                component.CanEditLabel,
                time,
                active != null,
                _accessReader.IsAllowed(args.User, uid)));
        }
    }

    /// <summary>
    ///     Finishes a timer, triggering its main port, and removing its <see cref="ActiveSignalTimerComponent"/>.
    /// </summary>
    public void Trigger(EntityUid uid, SignalTimerComponent signalTimer)
    {
        RemComp<ActiveSignalTimerComponent>(uid);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, new[] { signalTimer.Label }, appearance);
        }

        var announceMessage = signalTimer.Label;
        if (string.IsNullOrWhiteSpace(announceMessage)) { announceMessage = Loc.GetString("label-none"); }

        if (signalTimer.TimerCanAnnounce)
        {
            Report(uid, SignalTimerComponent.SecChannel, "timer-end-announcement", ("Label", announceMessage));
        }

        _audio.PlayPvs(signalTimer.DoneSound, uid);
        _signalSystem.InvokePort(uid, signalTimer.TriggerPort);

        if (_ui.TryGetUi(uid, SignalTimerUiKey.Key, out var bui))
        {
            _ui.SetUiState(bui, new SignalTimerBoundUserInterfaceState(signalTimer.Label,
                TimeSpan.FromSeconds(signalTimer.Delay).Minutes.ToString("D2"),
                TimeSpan.FromSeconds(signalTimer.Delay).Seconds.ToString("D2"),
                signalTimer.CanEditLabel,
                TimeSpan.Zero,
                false,
                true));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        _timers.Clear();

        var query = EntityQueryEnumerator<ActiveSignalTimerComponent, SignalTimerComponent>();
        while (query.MoveNext(out var uid, out var active, out var timer))
        {
            if (active.TriggerTime > _gameTiming.CurTime)
                continue;

            _timers.Add((uid, timer));
        }

        foreach (var timer in _timers)
        {
            // Exploded or the likes.
            if (!Exists(timer.Owner))
                continue;

            Trigger(timer.Owner, timer.Comp);
        }
    }

    /// <summary>
    ///     Checks if a UI <paramref name="message"/> is allowed to be sent by the user.
    /// </summary>
    /// <param name="uid">The entity that is interacted with.</param>
    private bool IsMessageValid(EntityUid uid, BaseBoundUserInterfaceEvent message)
    {
        if (message.Session.AttachedEntity is not { Valid: true } mob)
            return false;

        return _accessReader.IsAllowed(mob, uid);
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerTextChangedMessage"/> to both
    ///     change the default component label, and propagate that change to the TextScreen.
    /// </summary>
    private void OnTextChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerTextChangedMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        component.Label = args.Text[..Math.Min(5, args.Text.Length)];

        if (!HasComp<ActiveSignalTimerComponent>(uid))
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, new[] { component.Label });
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerDelayChangedMessage"/> to change the <see cref="SignalTimerComponent"/>
    ///     delay, and propagate that change to a textscreen.
    /// </summary>
    private void OnDelayChangedMessage(
        EntityUid uid,
        SignalTimerComponent component,
        SignalTimerDelayChangedMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        component.Delay = args.Delay.TotalSeconds;
        _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, component.Delay);
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerStartMessage"/> to instantiate an <see cref="ActiveSignalTimerComponent"/>,
    ///     clear <see cref="TextScreenVisuals.ScreenText"/>, propagate those changes, and invoke the start port.
    /// </summary>
    private void OnTimerStartMessage(EntityUid uid, SignalTimerComponent component, SignalTimerStartMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        OnStartTimer(uid, component);
    }

    private void OnSignalReceived(EntityUid uid, SignalTimerComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.Trigger)
        {
            OnStartTimer(uid, component);
        }
    }

    public void OnStartTimer(EntityUid uid, SignalTimerComponent component)
    {
        TryComp<AppearanceComponent>(uid, out var appearance);
        var timer = EnsureComp<ActiveSignalTimerComponent>(uid);
        timer.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Delay);

        if (appearance != null)
        {
            _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, timer.TriggerTime, appearance);
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, new string?[] { }, appearance);
        }

        _signalSystem.InvokePort(uid, component.StartPort);

        var announceMessage = component.Label;
        if (string.IsNullOrWhiteSpace(announceMessage)) { announceMessage = Loc.GetString("label-none"); }

        //sorry, skill issue
        var time = TimeSpan.FromSeconds(component.Delay);
        var timeFormatted =
            $"{(time.Duration().Hours > 0 ? $"{time.Hours:0} час;{(time.Hours == 1 ? string.Empty : "ов")} " : string.Empty)}{(time.Duration().Minutes > 0 ? $"{time.Minutes:0} минут;{(time.Minutes != 1 ? string.Empty : "а")} " : string.Empty)}{(time.Duration().Seconds > 0 ? $"{time.Seconds:0} секунд{(time.Seconds != 1 ? string.Empty : "а")} " : string.Empty)}";

        if (component.TimerCanAnnounce)
        {
            Report(uid, SignalTimerComponent.SecChannel, "timer-start-announcement", ("Label", announceMessage),
                ("Time", timeFormatted));
        }
    }

    private void Report(EntityUid source, string channelName, string messageKey, params (string, object)[] args)
    {
        var message = args.Length == 0 ? Loc.GetString(messageKey) : Loc.GetString(messageKey, args);
        var channel = _prototypeManager.Index<RadioChannelPrototype>(channelName);
        _radioSystem.SendRadioMessage(source, message, channel, source);
    }
}
