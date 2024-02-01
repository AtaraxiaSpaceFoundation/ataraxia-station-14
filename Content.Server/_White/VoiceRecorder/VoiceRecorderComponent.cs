using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.VoiceRecorder;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class VoiceRecorderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enabled")]
    public bool Enabled { get; set; } = true;

    [DataField("blacklist")]
    public EntityWhitelist Blacklist { get; private set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public int Range { get; private set; } = 10;

    [DataField("listening")]
    public bool Listening { get; set; } = false;

    /// <summary>
    /// The sound that's played when the scanner prints off a report.
    /// </summary>
    [DataField("soundPrint")]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    [DataField("soundEndOfRecording")]
    public SoundSpecifier SoundEndOfRecording = new SoundPathSpecifier("/Audio/Machines/id_insert.ogg");

    [DataField("soundStartOfRecording")]
    public SoundSpecifier SoundStartOfRecording = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/pistol_magin.ogg");

    /// <summary>
    /// What the machine will print
    /// </summary>
    [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MachineOutput = "PaperOffice";

    [DataField("recordings")]
    public List<string> Recordings = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maximumEntries")]
    public int MaximumEntries = 100;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("customTitle")]
    public string CustomTitle = "";

    /// <summary>
    /// When will the recorder be ready to print again?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;

    /// <summary>
    /// How often can the recorder print out reports?
    /// </summary>
    [DataField("printCooldown")]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);
}
