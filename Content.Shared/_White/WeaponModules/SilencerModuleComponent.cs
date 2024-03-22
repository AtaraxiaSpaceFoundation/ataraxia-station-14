using Robust.Shared.Audio;

namespace Content.Shared._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SilencerModuleComponent : WeaponModulesComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? OldSoundGunshot;
}
