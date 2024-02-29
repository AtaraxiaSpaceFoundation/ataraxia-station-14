using Robust.Shared.Prototypes;

namespace Content.Shared._White;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("animatedLobbyScreen")]
public sealed partial class AnimatedLobbyScreenPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("path")]
    public string Path { get; private set; } = string.Empty;
}
