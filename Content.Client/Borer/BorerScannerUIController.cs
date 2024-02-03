using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Borer;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Borer;


public sealed class BorerScannerUIController : UIController
{
    [Dependency] private readonly GameplayStateLoadController _gameplayStateLoad = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ScannerWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        _gameplayStateLoad.OnScreenLoad += LoadGui;
        _gameplayStateLoad.OnScreenUnload += UnloadGui;

        SubscribeNetworkEvent<BorerScanDoAfterEvent>(OpenWindow);

    }

    private void LoadGui()
    {
        DebugTools.Assert(_window == null);
        _window = UIManager.CreateWindow<ScannerWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);
    }

    private void UnloadGui()
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }
    }
    private void OpenWindow(BorerScanDoAfterEvent msg, EntitySessionEventArgs args)
    {
        var ent = _playerManager.LocalEntity;
        if (_window == null || _window.IsOpen || ent != args.SenderSession.AttachedEntity)
            return;
        _window.SolutionContainer.DisposeAllChildren();
        foreach (var reagent in msg.Solution)
        {
            var reagLabel = new Label();
            reagLabel.Text = reagent.Key + " - " + reagent.Value + "u";
            _window.SolutionContainer.Children.Add(reagLabel);
        }

        _window.Open();
    }

}
