using Content.Shared._White.Keyhole.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server._White.Construction;

[UsedImplicitly, DataDefinition]
public sealed partial class DoorUnlocked : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        return !entityManager.TryGetComponent(uid, out KeyholeComponent? keyhole) || !keyhole.Locked;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        if (Condition(args.Examined, IoCManager.Resolve<IEntityManager>()))
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-door-locked"));
        return true;

    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry
        {
            Localization = "construction-examine-condition-door-locked"
        };
    }
}
