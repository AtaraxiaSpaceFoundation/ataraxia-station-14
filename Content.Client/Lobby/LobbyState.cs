using System.Linq;
using System.Numerics;
using Content.Client._Ohio.Buttons;
using Content.Client.Audio;
using Content.Client.Changelog;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.Preferences;
using Content.Client.Preferences.UI;
using Content.Client.Resources;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.Voting;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Lobby
{
    public sealed class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ChangelogManager _changelog = default!;

        [ViewVariables] private CharacterSetupGui? _characterSetup;

        private ClientGameTicker _gameTicker = default!;
        private ContentAudioSystem _contentAudioSystem = default!;

        protected override Type? LinkedScreenType { get; } = typeof(LobbyGui);

        private LobbyGui? _lobby;

        protected override void Startup()
        {
            if (_userInterfaceManager.ActiveScreen == null)
            {
                return;
            }

            _lobby = (LobbyGui) _userInterfaceManager.ActiveScreen;

            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();

            _gameTicker = _entityManager.System<ClientGameTicker>();
            _contentAudioSystem = _entityManager.System<ContentAudioSystem>();
            _contentAudioSystem.LobbySoundtrackChanged += UpdateLobbySoundtrackInfo;
            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager,
                _prototypeManager, _configurationManager);

            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);

            _lobby.CharacterSetupState.AddChild(_characterSetup);

            chatController.SetMainChat(true);

            _voteManager.SetPopupContainer(_lobby.VoteContainer);

            _characterSetup.CloseButton.OnPressed += _ =>
            {
                // Reset sliders etc.
                _characterSetup?.UpdateControls();

                var controller = _userInterfaceManager.GetUIController<LobbyUIController>();
                controller.SetClothes(true);
                controller.UpdateProfile();
                _lobby.SwitchState(LobbyGui.LobbyGuiState.Default);
            };

            _characterSetup.SaveButton.OnPressed += _ =>
            {
                _characterSetup.Save();
                _userInterfaceManager.GetUIController<LobbyUIController>().ReloadProfile();
            };

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);

            _lobby.ServerName.Text = _baseClient.GameInfo?.ServerName; //The eye of refactor gazes upon you...

            UpdateLobbyUi();

            _lobby.CharacterSetupButton.OnPressed += OnSetupPressed;
            _lobby.ReadyButton.OnPressed += OnReadyPressed;
            _lobby.ReadyButton.OnToggled += OnReadyToggled;

            _gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;

            _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;
            _lobby.Background.RandomizeBackground();

            //_lobby.CharacterPreview.UpdateUI();
            PopulateChangelog();
        }

        protected override void Shutdown()
        {
            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();

            chatController.SetMainChat(false);

            _gameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;
            _contentAudioSystem.LobbySoundtrackChanged -= UpdateLobbySoundtrackInfo;

            _voteManager.ClearPopupContainer();

            _lobby!.CharacterSetupButton.OnPressed -= OnSetupPressed;
            _lobby!.ReadyButton.OnPressed -= OnReadyPressed;
            _lobby!.ReadyButton.OnToggled -= OnReadyToggled;

            _lobby = null;

            _characterSetup?.Dispose();
            _characterSetup = null;

            _preferencesManager.OnServerDataLoaded -= PreferencesDataLoaded;
        }

        private void PreferencesDataLoaded()
        {
            //_lobby?.CharacterPreview.UpdateUI();
        }

        private void OnSetupPressed(BaseButton.ButtonEventArgs args)
        {
            SetReady(false);
            _lobby!.SwitchState(LobbyGui.LobbyGuiState.CharacterSetup);
        }

        private void OnReadyPressed(BaseButton.ButtonEventArgs args)
        {
            if (!_gameTicker.IsGameStarted)
            {
                return;
            }

            new LateJoinGui().OpenCentered();
        }

        private void OnReadyToggled(BaseButton.ButtonToggledEventArgs args)
        {
            SetReady(args.Pressed);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_gameTicker.IsGameStarted)
            {
                _lobby!.StartTime.Text = string.Empty;
                var roundTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                _lobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-time",
                    ("hours", roundTime.Hours), ("minutes", roundTime.Minutes));

                return;
            }

            string text;

            if (_gameTicker.Paused)
            {
                text = Loc.GetString("lobby-state-paused");
            }
            else if (_gameTicker.StartTime < _gameTiming.CurTime)
            {
                _lobby!.StartTime.Text = Loc.GetString("lobby-state-soon");
                return;
            }
            else
            {
                var difference = _gameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                {
                    text = Loc.GetString(seconds < -5
                        ? "lobby-state-right-now-question"
                        : "lobby-state-right-now-confirmation");
                }
                else
                {
                    text = $"{difference.Minutes}:{difference.Seconds:D2}";
                }
            }

            _lobby!.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void LobbyStatusUpdated()
        {
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            _lobby!.ReadyButton.Disabled = _gameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_gameTicker.IsGameStarted)
            {
                MakeButtonJoinGame(_lobby!.ReadyButton);
                _lobby!.ReadyButton.ToggleMode = false;
                _lobby!.ReadyButton.Pressed = false;
                _lobby!.ObserveButton.Disabled = false;
            }
            else
            {
                _lobby!.StartTime.Text = string.Empty;

                if (_lobby!.ReadyButton.Pressed)
                    MakeButtonReady(_lobby!.ReadyButton);
                else
                    MakeButtonUnReady(_lobby!.ReadyButton);

                _lobby!.ReadyButton.ToggleMode = true;
                _lobby!.ReadyButton.Disabled = false;
                _lobby!.ReadyButton.Pressed = _gameTicker.AreWeReady;
                _lobby!.ObserveButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
            {
                _lobby!.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);
            }


            _lobby!.LabelName.SetMarkup("[font=\"Bedstead\" size=22] ATARAXIA [/font]");


            _lobby!.ChangelogLabel.SetMarkup("Список изменений:");
        }

        private void UpdateLobbySoundtrackInfo(LobbySoundtrackChangedEvent ev)
        {
            if (ev.SoundtrackFilename == null)
            {
                _lobby!.LobbySong.SetMarkup(Loc.GetString("lobby-state-song-no-song-text"));
            }
            else if (
                ev.SoundtrackFilename != null
                && _resourceCache.TryGetResource<AudioResource>(ev.SoundtrackFilename, out var lobbySongResource)
                )
            {
                var lobbyStream = lobbySongResource.AudioStream;

                var title = string.IsNullOrEmpty(lobbyStream.Title)
                    ? Loc.GetString("lobby-state-song-unknown-title")
                    : lobbyStream.Title;

                var artist = string.IsNullOrEmpty(lobbyStream.Artist)
                    ? Loc.GetString("lobby-state-song-unknown-artist")
                    : lobbyStream.Artist;

                var markup = Loc.GetString("lobby-state-song-text",
                    ("songTitle", title),
                    ("songArtist", artist));

                _lobby!.LobbySong.SetMarkup(markup);
            }
        }

        private void UpdateLobbyBackground()
        {
            if (_gameTicker.LobbyBackground != null)
            {
                _lobby!.Background.Texture = _resourceCache.GetResource<TextureResource>(_gameTicker.LobbyBackground );
            }
            else
            {
                _lobby!.Background.Texture = null;
            }

        }

        private void SetReady(bool newReady)
        {
            if (_gameTicker.IsGameStarted)
            {
                return;
            }

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
        }

        private void MakeButtonReady(OhioLobbyTextButton button)
        {
            button.ButtonText = "Готов";
        }

        private void MakeButtonUnReady(OhioLobbyTextButton button)
        {
            button.ButtonText = "Не готов";
        }

        private void MakeButtonJoinGame(OhioLobbyTextButton button)
        {
            button.ButtonText = "Присоединиться";
        }

        private async void PopulateChangelog()
        {
            if (_lobby?.ChangelogContainer?.Children is null)
            {
                return;
            }

            _lobby.ChangelogContainer.Children.Clear();

            var changelogs = await _changelog.LoadChangelog();
            var whiteChangelog = changelogs.Find(cl => cl.Name == "ChangelogWhite");

            if (whiteChangelog is null)
            {
                _lobby.ChangelogContainer.Children.Add(
                    new RichTextLabel().SetMarkup("Не удалось загрузить список изменений"));

                return;
            }

            var entries = whiteChangelog.Entries
                .OrderByDescending(c => c.Time)
                .Take(5);

            foreach (var entry in entries)
            {
                var box = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = Control.HAlignment.Left,
                    Children =
                    {
                        new Label
                        {
                            Align = Label.AlignMode.Left,
                            Text = $"{entry.Author} {entry.Time.ToShortDateString()}",
                            FontColorOverride = Color.FromHex("#888"),
                            Margin = new Thickness(0, 10)
                        }
                    }
                };

                foreach (var change in entry.Changes)
                {
                    var container = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        HorizontalAlignment = Control.HAlignment.Left
                    };

                    var text = new RichTextLabel();
                    text.SetMessage(FormattedMessage.FromMarkup(change.Message));
                    text.MaxWidth = 350;

                    container.AddChild(GetIcon(change.Type));
                    container.AddChild(text);

                    box.AddChild(container);
                }

                if (_lobby?.ChangelogContainer is null)
                {
                    return;
                }

                _lobby.ChangelogContainer.AddChild(box);
            }
        }

        private TextureRect GetIcon(ChangelogManager.ChangelogLineType type)
        {
            var (file, color) = type switch
            {
                ChangelogManager.ChangelogLineType.Add => ("plus.svg.192dpi.png", "#6ED18D"),
                ChangelogManager.ChangelogLineType.Remove => ("minus.svg.192dpi.png", "#D16E6E"),
                ChangelogManager.ChangelogLineType.Fix => ("bug.svg.192dpi.png", "#D1BA6E"),
                ChangelogManager.ChangelogLineType.Tweak => ("wrench.svg.192dpi.png", "#6E96D1"),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return new TextureRect
            {
                Texture = _resourceCache.GetTexture(new ResPath($"/Textures/Interface/Changelog/{file}")),
                VerticalAlignment = Control.VAlignment.Top,
                TextureScale = new Vector2(0.5f, 0.5f),
                Margin = new Thickness(2, 4, 6, 2),
                ModulateSelfOverride = Color.FromHex(color)
            };
        }
    }
}
