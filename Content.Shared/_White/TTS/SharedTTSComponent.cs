using Robust.Shared.Prototypes;

namespace Content.Shared._White.TTS;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
// ReSharper disable once InconsistentNaming
public sealed partial class SharedTTSComponent : Component
{
    /// <summary>
    /// Prototype of used voice for TTS.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public ProtoId<TTSVoicePrototype> VoicePrototypeId { get; set; } = "Eugene";
}