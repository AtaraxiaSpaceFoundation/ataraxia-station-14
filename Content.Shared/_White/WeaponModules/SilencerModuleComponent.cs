using Robust.Shared.Audio;

namespace Content.Shared._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SilencerModuleComponent : BaseModuleComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? OldSoundGunshot;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier NewSoundGunshot = new SoundPathSpecifier("/Audio/White/Weapons/Modules/silence.ogg");
}
