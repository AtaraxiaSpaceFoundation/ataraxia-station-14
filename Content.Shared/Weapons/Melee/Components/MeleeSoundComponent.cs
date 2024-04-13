using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// Plays the specified sound upon receiving damage of the specified type.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState] // WD
public sealed partial class MeleeSoundComponent : Component
{
    /// <summary>
    /// Specified sounds to apply when the entity takes damage with the specified group.
    /// Will fallback to defaults if none specified.
    /// </summary>
    [DataField("soundGroups",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<SoundSpecifier, DamageGroupPrototype>))]
    [AutoNetworkedField] // WD
    public Dictionary<string, SoundSpecifier>? SoundGroups;

    /// <summary>
    /// Specified sounds to apply when the entity takes damage with the specified type.
    /// Will fallback to defaults if none specified.
    /// </summary>
    [DataField("soundTypes",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<SoundSpecifier, DamageTypePrototype>))]
    [AutoNetworkedField] // WD
    public Dictionary<string, SoundSpecifier>? SoundTypes;

    /// <summary>
    /// Sound that plays if no damage is done.
    /// </summary>
    [AutoNetworkedField] // WD
    [DataField("noDamageSound")] public SoundSpecifier? NoDamageSound;
}
