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
    [Dependency] private readonly CriminalRecordsConsoleSystem _criminalRecordsConsoleSystem = default!;
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
                SetWanted(args.User, args.Target, ent.Value, component);
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

        if (!SetCriminalStatus(key, args.Status, uid, user, idCard.Comp, component.Reason, component.SecurityChannel))
        {
            _popupSystem.PopupEntity(Loc.GetString("security-hud-cant-set-status"), user, user, PopupType.Medium);
        }
    }

    private bool SetCriminalStatus(StationRecordKey key, SecurityStatus status, EntityUid hud, EntityUid officer,
        IdCardComponent idCard, string reason, string securityChannel)
    {
        if (!_stationRecordsSystem.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
            return false;

        if (!_stationRecordsSystem.TryGetRecord<CriminalRecord>(key, out var record) || record.Status == status)
            return false;

        var name = generalRecord.Name;
        var officerName = Loc.GetString("criminal-records-console-unknown-officer");
        if (_idCardSystem.TryFindIdCard(officer, out var id) && id.Comp.FullName is { } fullName)
            officerName = fullName;

        _criminalRecordsSystem.TryChangeStatus(key, status, reason);

        var locArgs = new (string, object)[] { ("name", name), ("officer", officerName), ("reason", reason) };

        var statusString = (record.Status, status) switch
        {
            (_, SecurityStatus.Detained) => "detained",
            (_, SecurityStatus.Suspected) => "suspected",
            (_, SecurityStatus.Paroled) => "paroled",
            (_, SecurityStatus.Discharged) => "released",
            (_, SecurityStatus.Wanted) => "wanted",
            (SecurityStatus.Suspected, SecurityStatus.None) => "not-suspected",
            (SecurityStatus.Wanted, SecurityStatus.None) => "not-wanted",
            (SecurityStatus.Detained, SecurityStatus.None) => "released",
            (SecurityStatus.Paroled, SecurityStatus.None) => "not-parole",
            _ => "not-wanted"
        };

        _radio.SendRadioMessage(hud, Loc.GetString($"criminal-records-console-{statusString}", locArgs), securityChannel, hud);
        _criminalRecordsConsoleSystem.UpdateCriminalIdentity(name, status);

        return true;
    }
}
