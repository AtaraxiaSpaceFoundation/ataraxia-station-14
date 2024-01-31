using Content.Shared.Miracle.UI;

namespace Content.Client.Miracle.Changeling.UI.TransformStingUI;

public sealed class TransformStingSelectorBui : BoundUserInterface
{
    private TransformStingSelectorWindow? _window;

    public TransformStingSelectorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new TransformStingSelectorWindow();
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.ItemSelected += (item, target) =>
        {
            var msg = new TransformStingItemSelectedMessage(item, target);
            SendMessage(msg);
        };

        if(State != null)
            UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is TransformStingBuiState newState)
        {
            _window?.PopulateList(newState.Items, newState.Target);
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
