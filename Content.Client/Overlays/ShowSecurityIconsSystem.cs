using Content.Client._White.EntityCrimeRecords;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared._White.CriminalRecords;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowSecurityIconsSystem : EquipmentHudSystem<ShowSecurityIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    // WD EDIT START
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ShowCrimeRecordsSystem _parentSystem = default!;
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
            Log.Error($"Invalid job icon prototype: {jobIcon}");

        if (TryComp<MindShieldComponent>(uid, out var comp))
        {
            if (_prototypeMan.TryIndex<StatusIconPrototype>(comp.MindShieldStatusIcon.Id, out var icon))
                result.Add(icon);
        }

        // WD EDIT START
        if (!GetRecord(uid, out var type))
            return result;

        var protoId = type switch
        {
            EnumCriminalRecordType.Discharged => "CriminalRecordIconDischarged",
            EnumCriminalRecordType.Incarcerated => "CriminalRecordIconIncarcerated",
            EnumCriminalRecordType.Parolled => "CriminalRecordIconParolled",
            EnumCriminalRecordType.Suspected => "CriminalRecordIconSuspected",
            EnumCriminalRecordType.Wanted => "CriminalRecordIconWanted",
            _ => "CriminalRecordIconReleased"
        };

        if (_prototypeMan.TryIndex<StatusIconPrototype>(protoId, out var recordIcon))
            result.Add(recordIcon);
        // WD EDIT END

        return result;
    }

    // WD EDIT START
    private bool GetRecord(EntityUid uid, out EnumCriminalRecordType type)
    {
        if (!_entManager.TryGetComponent(uid, out MetaDataComponent? meta))
        {
            type = EnumCriminalRecordType.Released;
            return false;
        }

        var serverList = _entManager.EntityQuery<CriminalRecordsServerComponent>();
        foreach (var server in serverList)
        {
            // if all good - check avaible records
            foreach (var (key, info) in server.Cache)
            {
                // Check id
                if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
                {
                    // PDA
                    if (_entManager.TryGetComponent(idUid, out PdaComponent? pda) &&
                        _entManager.TryGetComponent(pda.ContainedId, out IdCardComponent? idCard))
                    {
                        if (idCard.FullName == info.StationRecord.Name &&
                            idCard.JobTitle == info.StationRecord.JobTitle)
                        {
                            type = info.CriminalType;
                            return true;
                        }
                    }
                    // ID Card
                    if (_entManager.TryGetComponent(idUid, out IdCardComponent? id))
                    {
                        idCard = id;
                        if (idCard.FullName == info.StationRecord.Name &&
                            idCard.JobTitle == info.StationRecord.JobTitle)
                        {
                            type = info.CriminalType;
                            return true;
                        }
                    }
                }
                // Check DNA (Dirty Nanotrasen tehnology lol)
                // And yeah, he can't check - is pulled mask or not
                // it's only Content.Server logic, idk hot it impl to Content.Client
                if (_parentSystem.CanIdentityName(uid) != meta.EntityName)
                    continue;
                if (meta.EntityName != info.StationRecord.Name)
                    continue;
                type = info.CriminalType;
                return true;
            }
        }

        type = EnumCriminalRecordType.Released;
        return false;
    }
    // WD EDIT END
}
