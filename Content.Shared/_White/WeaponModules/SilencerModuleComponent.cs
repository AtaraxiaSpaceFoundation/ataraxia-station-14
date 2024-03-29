using Robust.Shared.Audio;

namespace Content.Shared._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SilencerModuleComponent : BaseModuleComponent
{
    public SoundSpecifier? OldSoundGunshot;

    public SoundSpecifier NewSoundGunshot = new SoundPathSpecifier("/Audio/White/Weapons/Modules/silence.ogg");
}
