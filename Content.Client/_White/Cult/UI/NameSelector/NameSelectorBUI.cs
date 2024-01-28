using Content.Shared._White.Cult.UI;

namespace Content.Client._White.Cult.UI.NameSelector;

public sealed class NameSelectorBUI : BoundUserInterface
{
    public NameSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private NameSelectorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OpenCentered();
        _window.OnNameChange += OnNameSelected;
        _window.OnClose += Close;
    }

    private void OnNameSelected(string name)
    {
        SendMessage(new NameSelectorMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NameSelectorBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Name);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
