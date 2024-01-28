using Content.Shared.Damage;

namespace Content.Server._White.Crossbow;

[RegisterComponent]
public sealed partial class ThrowDamageModifierComponent : Component
{
    public DamageSpecifier Damage = new();

    public bool AddEmbedding;

    public bool ClearDamageOnRemove;
}
