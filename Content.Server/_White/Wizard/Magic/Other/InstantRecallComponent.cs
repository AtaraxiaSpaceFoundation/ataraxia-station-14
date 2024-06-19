using Robust.Shared.Audio;

namespace Content.Server._White.Wizard.Magic.Other;

[RegisterComponent]
public sealed partial class InstantRecallComponent : Component
{
    public EntityUid? Item;

    /// <summary>
    /// Sound to play on use.
    /// </summary>
    [DataField]
    [ViewVariables]
    public SoundSpecifier LinkSound;

    /// <summary>
    /// Sound to play on use.
    /// </summary>
    [DataField]
    [ViewVariables]
    public SoundSpecifier RecallSound;
}
