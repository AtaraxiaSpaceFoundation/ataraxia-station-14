using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[Serializable, NetSerializable]
public sealed partial class AbsorbDnaDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class AbsorbDnaActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class TransformDoAfterEvent : SimpleDoAfterEvent
{
    public string SelectedDna;
}

public sealed partial class TransformActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RegenerateDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class RegenerateActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class LesserFormDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class LesserFormActionEvent : InstantActionEvent
{
}

public sealed partial class ExtractionStingActionEvent : EntityTargetActionEvent
{
}

public sealed partial class TransformStingActionEvent : EntityTargetActionEvent
{
}

public sealed partial class BlindStingActionEvent : EntityTargetActionEvent
{
}

public sealed partial class MuteStingActionEvent : EntityTargetActionEvent
{
}

public sealed partial class HallucinationStingActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CryoStingActionEvent : EntityTargetActionEvent
{
}

public sealed partial class AdrenalineSacsActionEvent : InstantActionEvent
{
}

public sealed partial class FleshmendActionEvent : InstantActionEvent
{
}

public sealed partial class ArmbladeActionEvent : InstantActionEvent
{
}

public sealed partial class OrganicShieldActionEvent : InstantActionEvent
{
}

public sealed partial class ChitinousArmorActionEvent : InstantActionEvent
{
}

public sealed partial class HiveHeadActionEvent : InstantActionEvent
{
}

public sealed partial class ReleaseBeesEvent : InstantActionEvent
{
}

public sealed partial class TentacleArmActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingShopActionEvent : InstantActionEvent
{
}

public sealed partial class BiodegradeActionEvent : InstantActionEvent
{
}

public sealed partial class AugmentedEyesightActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AugmentedEyesightPurchasedEvent : EntityEventArgs
{
}

public sealed partial class DissonantShriekActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VoidAdaptationPurchasedEvent : EntityEventArgs
{
}

public sealed class ChemRegenModifyEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public float Multiplier = 1f;
}

public sealed class ChangelingRefundEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;

    public EntityUid Store;
}
