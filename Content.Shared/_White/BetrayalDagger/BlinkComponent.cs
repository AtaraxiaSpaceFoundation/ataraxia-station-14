using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._White.BetrayalDagger;

[RegisterComponent]
public sealed partial class BlinkComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Distance = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BlinkRate = 1f;

    public TimeSpan NextBlink;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f)
    };
}

[Serializable, NetSerializable]
public sealed class BlinkEvent : EntityEventArgs
{
    public readonly NetEntity Weapon;
    public readonly Vector2 Direction;

    public BlinkEvent(NetEntity weapon, Vector2 direction)
    {
        Weapon = weapon;
        Direction = direction;
    }
}
