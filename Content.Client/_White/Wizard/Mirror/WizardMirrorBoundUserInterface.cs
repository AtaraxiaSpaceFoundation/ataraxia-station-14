using Content.Shared._White.Wizard.Mirror;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Wizard.Mirror;

public sealed class WizardMirrorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [ViewVariables]
    private WizardMirrorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new(_prototypeManager);

        _window.OnSave += Save;

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    private void Save(HumanoidCharacterProfile profile)
    {
        SendMessage(new WizardMirrorSave(profile));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not WizardMirrorUiState data || _window == null)
            return;

        _window.UpdateState(data);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }
}

