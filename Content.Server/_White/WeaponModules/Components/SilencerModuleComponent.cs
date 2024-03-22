using Robust.Shared.Audio;

namespace Content.Server._White.WeaponModules;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SilencerModuleComponent : Shared._White.WeaponModules.WeaponModulesComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? OldSoundGunshot;
}
