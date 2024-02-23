using Robust.Shared.Audio;

namespace Content.Shared._White.Keyhole.Components;

[RegisterComponent]
public sealed partial class KeyformComponent : KeyBaseComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool IsUsed;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public SoundSpecifier PressSound = new SoundPathSpecifier("/Audio/White/Object/Tools/Form/press.ogg");
}
