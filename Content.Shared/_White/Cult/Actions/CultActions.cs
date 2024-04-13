using Content.Shared.Actions;

namespace Content.Shared._White.Cult.Actions;

public sealed partial class CultTwistedConstructionActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultSummonDaggerActionEvent : InstantActionEvent
{
}

public sealed partial class CultStunTargetActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultTeleportTargetActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultElectromagneticPulseInstantActionEvent : InstantActionEvent
{
}

public sealed partial class CultShadowShacklesTargetActionEvent : EntityTargetActionEvent
{
}

public sealed partial class CultSummonCombatEquipmentTargetActionEvent : EntityTargetActionEvent
{
}

[Virtual]
public partial class CultConcealPresenceInstantActionEvent : InstantActionEvent
{
}

public sealed partial class CultConcealInstantActionEvent : CultConcealPresenceInstantActionEvent
{
}

public sealed partial class CultRevealInstantActionEvent : CultConcealPresenceInstantActionEvent
{
}

public sealed partial class CultBloodRitesInstantActionEvent : InstantActionEvent
{
}

public sealed partial class CultBloodSpearRecallInstantActionEvent : InstantActionEvent
{
}
