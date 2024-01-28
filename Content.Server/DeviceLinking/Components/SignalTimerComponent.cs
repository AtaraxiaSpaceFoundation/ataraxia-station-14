using Content.Shared.DeviceLinking;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components;

[RegisterComponent]
public sealed partial class SignalTimerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double Delay = 5;

    /// <summary>
    ///     This shows the Label: text box in the UI.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanEditLabel = true;

    /// <summary>
    ///     The label, used for TextScreen visuals currently.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Label = string.Empty;

    /// <summary>
    ///     The port that gets signaled when the timer triggers.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> TriggerPort = "Timer";

    /// <summary>
    ///     The port that gets signaled when the timer starts.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> StartPort = "Start";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> Trigger = "Trigger";

    /// <summary>
    ///     If not null, this timer will play this sound when done.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? DoneSound;
    [DataField("timerCanAnnounce")]

    [ViewVariables(VVAccess.ReadWrite)]
    public bool TimerCanAnnounce;

    [DataField("SecChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public static string SecChannel = "Security";
}

