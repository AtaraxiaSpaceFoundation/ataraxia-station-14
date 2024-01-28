using System.Numerics;
using Content.Shared.Damage;

namespace Content.Server._White.Crossbow;

[RegisterComponent]
public sealed partial class ModifyDamageOnShootComponent : Component
{
    [DataField("damage", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField("addEmbedding")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AddEmbedding;

    [DataField("offset")]
    public Vector2 Offset = Vector2.Zero;
}
