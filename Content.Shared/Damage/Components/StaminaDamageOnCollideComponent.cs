using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when colliding with an entity.
/// </summary>
[RegisterComponent]
public sealed partial class StaminaDamageOnCollideComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 55f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool IgnoreResistances = true;

    [DataField("sound")]
    public SoundSpecifier? Sound;
}
