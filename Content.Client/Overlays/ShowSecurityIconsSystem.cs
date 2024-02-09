using System.Linq;
using Content.Shared._Miracle.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.Security;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowSecurityIconsSystem : EquipmentHudSystem<ShowSecurityIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    // WD EDIT START
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    // WD EDIT END

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string JobIconForNoId = "JobIconNoId";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, StatusIconComponent _, ref GetStatusIconsEvent @event)
    {
        if (!IsActive || @event.InContainer)
        {
            return;
        }

        var securityIcons = DecideSecurityIcon(uid);

        @event.StatusIcons.AddRange(securityIcons);
    }

    private IReadOnlyList<StatusIconPrototype> DecideSecurityIcon(EntityUid uid)
    {
        var result = new List<StatusIconPrototype>();

        var jobIconToGet = JobIconForNoId;
        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp(item, out IdCardComponent? id))
                {
                    jobIconToGet = id.JobIcon;
                    break;
                }

                // PDA
                if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    jobIconToGet = id.JobIcon;
                    break;
                }
            }
        }

        if (_prototypeMan.TryIndex<StatusIconPrototype>(jobIconToGet, out var jobIcon))
            result.Add(jobIcon);
        else
        // WD EDIT START
        {
            Log.Error($"Invalid job icon prototype: {jobIconToGet}");
            result.Add(_prototypeMan.Index<StatusIconPrototype>(JobIconForNoId));
        }
        // WD EDIT END

        if (TryComp<MindShieldComponent>(uid, out var comp))
        {
            if (_prototypeMan.TryIndex<StatusIconPrototype>(comp.MindShieldStatusIcon.Id, out var icon))
                result.Add(icon);
        }

        // WD EDIT START
        string? protoId;
        switch (GetRecord(uid))
        {
            case SecurityStatus.Detained:
                protoId = "CriminalRecordIconIncarcerated";
                break;
            case SecurityStatus.Released:
                protoId = "CriminalRecordIconReleased";
                break;
            case SecurityStatus.Suspected:
                protoId = "CriminalRecordIconSuspected";
                break;
            case SecurityStatus.Wanted:
                protoId = "CriminalRecordIconWanted";
                break;
            case SecurityStatus.None:
            default:
                return result;
        }

        if (_prototypeMan.TryIndex<StatusIconPrototype>(protoId, out var recordIcon))
            result.Add(recordIcon);
        // WD EDIT END

        return result;
    }

    // WD EDIT START
    private SecurityStatus GetRecord(EntityUid uid)
    {
        if (!_entManager.TryGetComponent(uid, out MetaDataComponent? meta))
            return SecurityStatus.None;

        var name = meta.EntityName;

        var ev = new SeeIdentityAttemptEvent();
        RaiseLocalEvent(uid, ev);

        if (ev.Cancelled)
        {
            if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid) &&
                _idCard.TryGetIdCard(idUid.Value, out var idCard))
                name = idCard.Comp.FullName;
            else
                return SecurityStatus.None;
        }

        if (name == null)
            return SecurityStatus.None;

        var query = EntityQuery<CriminalStatusDataComponent>();
        foreach (var data in query)
        {
            if (data.Statuses.TryGetValue(name, out var status))
                return status;
        }

        return SecurityStatus.None;
    }
    // WD EDIT END
}
