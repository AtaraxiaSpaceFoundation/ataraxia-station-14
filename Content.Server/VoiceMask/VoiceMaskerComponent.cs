using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class VoiceMaskerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public string LastSetName = "Unknown";

    [ViewVariables(VVAccess.ReadWrite)]
    public string? LastSetVoice; // tts

    [DataField]
    public ProtoId<SpeechVerbPrototype>? LastSpeechVerb;

    [DataField]
    public EntProtoId Action = "ActionChangeVoiceMask";

    [DataField]
    public EntityUid? ActionEntity;
}