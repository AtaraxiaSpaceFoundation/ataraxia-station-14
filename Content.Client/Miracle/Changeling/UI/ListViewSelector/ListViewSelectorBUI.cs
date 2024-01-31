using Content.Shared.Miracle.UI;

namespace Content.Client.Miracle.Changeling.UI.ListViewSelector;

public sealed class ListViewSelectorBui : BoundUserInterface
{
    private ListViewChangelingSelectorWindow? _window;

    public ListViewSelectorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new ListViewChangelingSelectorWindow();
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.ItemSelected += item =>
        {
            var msg = new ListViewItemSelectedMessage(item);
            SendMessage(msg);
        };

        if(State != null)
            UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ListViewBuiState newState)
        {
            _window?.PopulateList(newState.Items);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Close();
    }
}
