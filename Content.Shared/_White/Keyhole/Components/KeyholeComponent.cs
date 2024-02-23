using Robust.Shared.Audio;

namespace Content.Shared._White.Keyhole.Components;

[RegisterComponent]
public sealed partial class KeyholeComponent: KeyBaseComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Locked = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Delay = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public SoundSpecifier DoorLockedSound = new SoundPathSpecifier("/Audio/White/Object/Tools/Keyhole/locked.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public SoundSpecifier UnlockSound = new SoundPathSpecifier("/Audio/White/Object/Tools/Keyhole/unlock.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/White/Object/Tools/Keyhole/lock.ogg");
}
