using System.Linq;
using Content.Client._White.UserInterface.Radial;
using Content.Shared._White.SecurityHud;
using Content.Shared.Security;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client._White.SecurityHud;

public sealed class SecurityHudBUI : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private RadialContainer? _radialContainer;

    private bool _updated;

    private readonly Dictionary<string, string> _names = new()
    {
        { "SecurityIconDischarged", Loc.GetString("criminal-records-status-released")},
        { "SecurityIconSuspected", Loc.GetString("criminal-records-status-suspected")},
        { "SecurityIconWanted", Loc.GetString("criminal-records-status-wanted")},
        { "SecurityIconIncarcerated", Loc.GetString("criminal-records-status-detained")},
        { "CriminalRecordIconRemove", Loc.GetString("security-hud-remove-status") }
    };

    private readonly Dictionary<string, string> _icons = new()
    {
        { "SecurityIconDischarged", "/Textures/White/Interface/securityhud.rsi/released.png" },
        { "SecurityIconSuspected", "/Textures/White/Interface/securityhud.rsi/suspected.png" },
        { "SecurityIconWanted", "/Textures/White/Interface/securityhud.rsi/wanted.png" },
        { "SecurityIconIncarcerated", "/Textures/White/Interface/securityhud.rsi/incarcerated.png" },
        { "CriminalRecordIconRemove", "/Textures/White/Interface/securityhud.rsi/remove.png" }
    };

    private readonly Dictionary<string, SecurityStatus> _status = new()
    {
        { "SecurityIconDischarged", SecurityStatus.Discharged },
        { "SecurityIconSuspected", SecurityStatus.Suspected },
        { "SecurityIconWanted", SecurityStatus.Wanted },
        { "SecurityIconIncarcerated", SecurityStatus.Detained },
        { "CriminalRecordIconRemove", SecurityStatus.None }
    };


    public SecurityHudBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (_radialContainer != null)
            UIReset();

        _radialContainer = new RadialContainer();

        _radialContainer.Closed += Close;

        if (State != null)
            UpdateState(State);
    }

    private void UIReset()
    {
        _radialContainer?.Close();
        _radialContainer = null;
        _updated = false;
    }

    private void PopulateRadial(IReadOnlyCollection<string> ids, NetEntity user, NetEntity target)
    {
        foreach (var id in ids)
        {
            if (_radialContainer == null)
                continue;

            if(!_names.TryGetValue(id, out var name) || !_icons.TryGetValue(id, out var icon) || !_status.TryGetValue(id, out var status))
                return;

            var button = _radialContainer.AddButton(name, icon);
            button.Controller.OnPressed += _ =>
            {
                Select(status, user, target);
            };
        }
    }

    private void Select(SecurityStatus status, NetEntity user, NetEntity target)
    {
        SendMessage(new SecurityHudStatusSelectedMessage(status, user, target));
        UIReset();
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        UIReset();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_updated)
            return;

        if (state is SecurityHudBUIState newState)
        {
            PopulateRadial(newState.Ids, newState.User, newState.Target);
        }

        if (_radialContainer == null)
            return;

        _radialContainer?.OpenAttachedLocalPlayer();
        _updated = true;
    }
}
