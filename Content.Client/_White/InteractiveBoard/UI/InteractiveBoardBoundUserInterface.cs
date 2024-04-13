using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using static Content.Shared._White.InteractiveBoard.SharedInteractiveBoardComponent;

namespace Content.Client._White.InteractiveBoard.UI;

[UsedImplicitly]
public sealed class InteractiveBoardBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private InteractiveBoardWindow? _window;

    public InteractiveBoardBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new InteractiveBoardWindow();
        _window.OnClose += Close;
        _window.Input.OnKeyBindDown += args =>
        {
            if (args.Function == EngineKeyFunctions.TextSubmit)
            {
                var text = Rope.Collapse(_window.Input.TextRope);
                Input_OnTextEntered(text);
                args.Handle();
            }
        };

        if (EntMan.TryGetComponent<InteractiveBoardVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.Populate((InteractiveBoardBoundUserInterfaceState) state);
    }

    private void Input_OnTextEntered(string text)
    {
        SendMessage(new InteractiveBoardInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}
