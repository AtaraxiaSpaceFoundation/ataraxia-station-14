using Content.Client.Actions;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Actions;
using Content.Shared.Borer;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client.Borer;


public sealed class ReagentUIController : UIController, IOnSystemChanged<ActionsSystem>, IOnStateEntered<GameplayState>
{
    [Dependency] private readonly GameplayStateLoadController _gameplayStateLoad = default!;
    [UISystemDependency] private readonly SharedBorerSystem _borerSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ReagentWindow? _window;

    private bool _reagentsLoaded = false;

    public override void Initialize()
    {
        base.Initialize();


        _gameplayStateLoad.OnScreenLoad += LoadGui;
        _gameplayStateLoad.OnScreenUnload += UnloadGui;

        SubscribeLocalEvent<BorerInjectWindowOpenEvent>(ev =>
        {
            OpenWindow();
        });
    }

    private void LoadGui()
    {
        DebugTools.Assert(_window == null);
        _window = UIManager.CreateWindow<ReagentWindow>();
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

    private void OnInjectReagent(string protoId, int cost)
    {
        _borerSystem.RaiseInjectEvent(protoId, cost);
    }
    private void OpenWindow()
    {
        var ent = _playerManager.LocalEntity;
        if (_window == null || _window.IsOpen || !ent.HasValue)
            return;
        if (!_reagentsLoaded)
        {
            foreach (var reagent in _borerSystem.GetReagents(ent.Value))
            {
                var button = new Button();
                button.Text = Loc.GetString("borer-ui-secrete-inject-label",
                    ("reagent",Loc.GetString("reagent-name-"+
                    reagent.Key.ToLower().Replace("spacedrugs", "space-drugs"))),
                    ("cost", reagent.Value));
                button.OnPressed += _ => OnInjectReagent(reagent.Key, reagent.Value);
                _window.MainContainer.AddChild(button);
            }
            _reagentsLoaded = true;
        }

        _window.Open();
    }

    public void OnSystemLoaded(ActionsSystem system)
    {
        system.LinkActions += OnComponentLinked;
    }

    public void OnSystemUnloaded(ActionsSystem system)
    {
        system.LinkActions -= OnComponentLinked;
    }

    private void OnComponentLinked(ActionsComponent component)
    {
    }

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenActionsMenu, InputCmdHandler.FromDelegate(_ => OpenWindow()))
            .Register<ReagentWindow>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<ReagentWindow>();
    }
}
