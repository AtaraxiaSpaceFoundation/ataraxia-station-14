using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Wizard.ScrollSystem;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedScrollSystem))]
public sealed partial class ScrollComponent : Component
{
    /// <summary>
    /// ActionId to give on use.
    /// </summary>
    [DataField]
    [ViewVariables]
    public string ActionId;

    /// <summary>
    /// How time it takes to learn.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float LearnTime = 5f;

    /// <summary>
    /// Popup on learn.
    /// </summary>
    [DataField]
    [ViewVariables]
    public string LearnPopup;

    /// <summary>
    /// Sound to play on use.
    /// </summary>
    [DataField]
    [ViewVariables]
    public SoundSpecifier UseSound;

    /// <summary>
    /// Sound to play after use.
    /// </summary>
    [DataField]
    [ViewVariables]
    public SoundSpecifier AfterUseSound;
}
