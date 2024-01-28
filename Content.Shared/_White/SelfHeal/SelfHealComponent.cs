using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._White.SelfHeal;

[RegisterComponent]
public sealed partial class SelfHealComponent: Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("delay")]
    public float Delay = 3f;

    [ViewVariables(VVAccess.ReadWrite), DataField("healingSound")]
    public SoundSpecifier? HealingSound;

    [ViewVariables(VVAccess.ReadWrite), DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageContainers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
    public List<string>? DamageContainers;

    [ViewVariables(VVAccess.ReadWrite),DataField("disallowedClothingUser")]
    public List<string>? DisallowedClothingUser;

    [ViewVariables(VVAccess.ReadWrite), DataField("disallowedClothingTarget")]
    public List<string>? DisallowedClothingTarget;

    [DataField]
    public EntProtoId Action = "SelfHealAction";

    [DataField]
    public EntityUid? ActionEntity;
}

public sealed partial class SelfHealEvent : EntityTargetActionEvent
{
}
