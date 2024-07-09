using Robust.Shared.Audio;

namespace Content.Server._White.Other.DeathGasps;

[RegisterComponent]
public sealed partial class DeathGaspsComponent : Component
{
    [DataField]
    public SoundSpecifier DeathSounds = new SoundCollectionSpecifier("deathSounds");

    [DataField]
    public SoundSpecifier HeartSounds = new SoundCollectionSpecifier("heartSounds");

    [DataField]
    public bool CanOtherHearDeathSound;
}
