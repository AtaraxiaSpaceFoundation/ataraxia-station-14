using Content.Client.UserInterface.Systems.EscapeMenu;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Lobby.UI
{
    [GenerateTypedNameReferences]
    internal sealed partial class LobbyGui : UIScreen
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        public LobbyGui()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
            SetAnchorPreset(MainContainer, LayoutPreset.Wide);
            SetAnchorPreset(Background, LayoutPreset.Wide);

            OptionsButton.OnPressed += _ => _userInterfaceManager.GetUIController<OptionsUIController>().ToggleWindow();
            QuitButton.OnPressed += _ => _consoleHost.ExecuteCommand("disconnect");
        }

        public void SwitchState(LobbyGuiState state)
        {
            switch (state)
            {
                case LobbyGuiState.Default:
                    CharacterSetupState.Visible = false;
                    Center.Visible = true;
                    RightSide.Visible = true;
                    Version.Visible = true;
                    LabelName.Visible = true;
                    Changelog.Visible = true;
                    break;
                case LobbyGuiState.CharacterSetup:
                    CharacterSetupState.Visible = true;
                    Center.Visible = false;
                    RightSide.Visible = false;
                    Version.Visible = false;
                    LabelName.Visible = false;
                    Changelog.Visible = false;
                    break;
            }
        }

        public enum LobbyGuiState : byte
        {
            /// <summary>
            ///  The default state, i.e., what's seen on launch.
            /// </summary>
            Default,
            /// <summary>
            ///  The character setup state.
            /// </summary>
            CharacterSetup
        }
    }
}
