using Content.Server.Access.Systems;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.StationRecords.Systems;
using Content.Shared._Miracle.Components;
using Content.Shared._White.SecurityHud;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Security;
using Content.Shared.Security.Components;
using Content.Shared.StationRecords;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._White.SecurityHud;

public sealed class SecurityHudSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecordsSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly InventorySystem _invSlotsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<SecurityHudComponent, SecurityHudStatusSelectedMessage>(OnStatusSelected);
    }

    private void OnAltVerb(GetVerbsEvent<AlternativeVerb> args)
    {
        if(!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        if(!_invSlotsSystem.TryGetSlotEntity(args.User, "eyes", out var ent))
            return;

        if(!TryComp<SecurityHudComponent>(ent, out var component))
            return;

        if(!TryComp<AccessReaderComponent>(ent, out var accessReaderComponent))
            return;

        if (!_accessReaderSystem.IsAllowed(args.User, (EntityUid) ent, accessReaderComponent))
        {
            _popupSystem.PopupEntity(Loc.GetString("security-hud-not-allowed"), args.User, args.User, PopupType.Medium);
            return;
        }

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                SetWanted(args.User, args.Target, (EntityUid) ent, component);
            },
            Disabled = false,
            Priority = 0,
            Text = Loc.GetString("security-hud-verb"),
        };

        args.Verbs.Add(verb);
    }

    private void SetWanted(EntityUid uid, EntityUid target, EntityUid hud, SecurityHudComponent component)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        if (_ui.TryGetUi(hud, SecurityHudUiKey.Key, out var bui))
        {
            _ui.SetUiState(bui, new SecurityHudBUIState(component.Status, GetNetEntity(uid), GetNetEntity(target)));
            _ui.OpenUi(bui, actor.PlayerSession);
        }
    }

    private void OnStatusSelected(EntityUid uid, SecurityHudComponent component, SecurityHudStatusSelectedMessage args)
    {
        var user = GetEntity(args.User);
        var target = GetEntity(args.Target);

        if (!_idCardSystem.TryFindIdCard(target, out var idCard))
        {
            _popupSystem.PopupEntity(Loc.GetString("security-hud-id-unknown"), user, user, PopupType.Medium);
            return;
        }

        if(!TryComp<StationRecordKeyStorageComponent>(idCard, out var stationRecordKeyComp))
            return;

        if (stationRecordKeyComp.Key == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("security-hud-key-null"), user, user, PopupType.Medium);
            return;
        }

        var key = stationRecordKeyComp.Key.Value;

        if (!SetCriminalStatus(key, args.Status, user, idCard.Comp, component.Reason, component.SecurityChannel))
        {
            _popupSystem.PopupEntity(Loc.GetString("security-hud-cant-set-status"), user, user, PopupType.Medium);
        }
    }

    private bool SetCriminalStatus(StationRecordKey key, SecurityStatus status, EntityUid hud, IdCardComponent idCard, string reason, string securityChannel)
    {
        if (!_stationRecordsSystem.TryGetRecord<GeneralStationRecord>(key, out var rec))
            return false;

        var name = string.Empty;
        reason = idCard.FullName != null ? $"{reason} ({idCard.FullName})" : reason;

        if (!_stationRecordsSystem.TryGetRecord<CriminalRecord>(key, out var record) || record.Status == status)
            return false;

        if (_stationRecordsSystem.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
            name = generalRecord.Name;

        _criminalRecordsSystem.TryChangeStatus(key, status, reason);

        var locArgs = new (string, object)[] { ("name", name), ("officer", idCard.FullName)!, ("reason", reason) };

        var statusString = (record.Status, status) switch
        {
            (_, SecurityStatus.Detained) => "detained",
            (SecurityStatus.Detained, SecurityStatus.None) => "released",
            (_, SecurityStatus.None) => "not-wanted",
            (_, SecurityStatus.Wanted) => "wanted",
            (_, SecurityStatus.Discharged) => "released",
            (_, SecurityStatus.Suspected) => "suspected",
            _ => "not-wanted"
        };

        _radio.SendRadioMessage(hud, Loc.GetString($"criminal-records-console-{statusString}", locArgs), securityChannel, hud);

        var criminalData = EnsureComp<CriminalRecordComponent>(hud);

        criminalData.StatusIcon = status switch
        {
            SecurityStatus.Detained => "SecurityIconIncarcerated",
            SecurityStatus.None => "CriminalRecordIconRemove",
            SecurityStatus.Wanted => "SecurityIconWanted",
            SecurityStatus.Discharged => "SecurityIconDischarged",
            SecurityStatus.Suspected => "SecurityIconSuspected",
            _ => criminalData.StatusIcon
        };

        Dirty(hud, criminalData);

        return true;
    }
}
