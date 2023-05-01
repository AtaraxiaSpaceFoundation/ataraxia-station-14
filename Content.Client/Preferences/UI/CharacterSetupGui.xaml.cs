using System.Linq;
using System.Numerics;
using Content.Client.Humanoid;
using Content.Client.Info;
using Content.Client.Info.PlaytimeStats;
using Content.Client.Lobby.UI;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Preferences.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class CharacterSetupGui : Control
    {
        private readonly IClientPreferencesManager _preferencesManager;
        private readonly IEntityManager _entityManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly IConfigurationManager _configurationManager;
        private readonly Button _createNewCharacterButton;
        private readonly HumanoidProfileEditor _humanoidProfileEditor;

        public CharacterSetupGui(
            IEntityManager entityManager,
            IResourceCache resourceCache,
            IClientPreferencesManager preferencesManager,
            IPrototypeManager prototypeManager,
            IConfigurationManager configurationManager)
        {
            RobustXamlLoader.Load(this);
            _entityManager = entityManager;
            _prototypeManager = prototypeManager;
            _preferencesManager = preferencesManager;
            _configurationManager = configurationManager;

            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = new Color(37, 42, 37)
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            BackgroundPanel.PanelOverride = back;

            _createNewCharacterButton = new Button
            {
                Text = Loc.GetString("character-setup-gui-create-new-character-button"),
            };
            _createNewCharacterButton.OnPressed += args =>
            {
                preferencesManager.CreateCharacter(HumanoidCharacterProfile.Random());
                UpdateUI();
                args.Event.Handle();
            };

            _humanoidProfileEditor = new HumanoidProfileEditor(preferencesManager, prototypeManager, entityManager, configurationManager);
            _humanoidProfileEditor.OnProfileChanged += ProfileChanged;
            CharEditor.AddChild(_humanoidProfileEditor);

            UpdateUI();

            RulesButton.OnPressed += _ => new RulesAndInfoWindow().Open();

            StatsButton.OnPressed += _ => new PlaytimeStatsWindow().OpenCentered();
            preferencesManager.OnServerDataLoaded += UpdateUI;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _preferencesManager.OnServerDataLoaded -= UpdateUI;
        }

        public void Save() => _humanoidProfileEditor.Save();

        private void ProfileChanged(ICharacterProfile profile, int profileSlot)
        {
            _humanoidProfileEditor.UpdateControls();
            UpdateUI();
        }

        private void UpdateUI()
        {
            var numberOfFullSlots = 0;
            var characterButtonsGroup = new ButtonGroup();
            Characters.RemoveAllChildren();

            if (!_preferencesManager.ServerDataLoaded)
            {
                return;
            }

            _createNewCharacterButton.ToolTip =
                Loc.GetString("character-setup-gui-create-new-character-button-tooltip",
                ("maxCharacters", _preferencesManager.Settings!.MaxCharacterSlots));

            var isDisplayedMaxSlots = false;
            foreach (var (slot, character) in _preferencesManager.Preferences!.Characters)
            {
                if (character is null)
                {
                    continue;
                }

                isDisplayedMaxSlots = numberOfFullSlots >= _preferencesManager.Settings.MaxCharacterSlots;
                if (isDisplayedMaxSlots)
                    break;

                numberOfFullSlots++;
                var characterPickerButton = new CharacterPickerButton(_entityManager,
                    _preferencesManager,
                    _prototypeManager,
                    characterButtonsGroup,
                    character);
                Characters.AddChild(characterPickerButton);

                var characterIndexCopy = slot;
                characterPickerButton.OnPressed += args =>
                {
                    _humanoidProfileEditor.Profile = (HumanoidCharacterProfile)character;
                    _humanoidProfileEditor.CharacterSlot = characterIndexCopy;
                    _humanoidProfileEditor.UpdateControls();
                    _preferencesManager.SelectCharacter(character);
                    UpdateUI();
                    args.Event.Handle();
                };
            }

            _createNewCharacterButton.Disabled = isDisplayedMaxSlots;
            Characters.AddChild(_createNewCharacterButton);
        }

        private sealed class CharacterPickerButton : ContainerButton
        {
            private EntityUid _previewDummy;

            public CharacterPickerButton(
                IEntityManager entityManager,
                IClientPreferencesManager preferencesManager,
                IPrototypeManager prototypeManager,
                ButtonGroup group,
                ICharacterProfile profile)
            {
                AddStyleClass(StyleClassButton);
                ToggleMode = true;
                Group = group;

                var humanoid = profile as HumanoidCharacterProfile;
                if (humanoid is not null)
                {
                    var dummy = prototypeManager.Index<SpeciesPrototype>(humanoid.Species).DollPrototype;
                    _previewDummy = entityManager.SpawnEntity(dummy, MapCoordinates.Nullspace);
                }
                else
                {
                    _previewDummy = entityManager.SpawnEntity(prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
                }

                EntitySystem.Get<HumanoidAppearanceSystem>().LoadProfile(_previewDummy, (HumanoidCharacterProfile)profile);

                if (humanoid != null)
                {
                    LobbyCharacterPreviewPanel.GiveDummyJobClothes(_previewDummy, humanoid);
                }

                var isSelectedCharacter = profile == preferencesManager.Preferences?.SelectedCharacter;

                if (isSelectedCharacter)
                    Pressed = true;

                var view = new SpriteView
                {
                    Scale = new Vector2(2, 2),
                    OverrideDirection = Direction.South
                };
                view.SetEntity(_previewDummy);

                var description = profile.Name;

                var highPriorityJob = humanoid?.JobPriorities.SingleOrDefault(p => p.Value == JobPriority.High).Key;
                if (highPriorityJob != null)
                {
                    var jobName = IoCManager.Resolve<IPrototypeManager>().Index<JobPrototype>(highPriorityJob).LocalizedName;
                    description = $"{description}\n{jobName}";
                }

                var descriptionLabel = new Label
                {
                    Text = description,
                    ClipText = true,
                    HorizontalExpand = true
                };
                var deleteButton = new Button
                {
                    Text = Loc.GetString("character-setup-gui-character-picker-button-delete-button"),
                    Visible = !isSelectedCharacter,
                };
                var confirmDeleteButton = new Button
                {
                    Text = Loc.GetString("character-setup-gui-character-picker-button-confirm-delete-button"),
                    Visible = false,
                };
                confirmDeleteButton.ModulateSelfOverride = StyleNano.ButtonColorCautionDefault;
                confirmDeleteButton.OnPressed += _ =>
                {
                    Parent?.RemoveChild(this);
                    Parent?.RemoveChild(confirmDeleteButton);
                    preferencesManager.DeleteCharacter(profile);
                };
                deleteButton.OnPressed += _ =>
                {

                    deleteButton.Visible = false;
                    confirmDeleteButton.Visible = true;

                };

                var internalHBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    SeparationOverride = 0,
                    Children =
                    {
                        view,
                        descriptionLabel,
                        deleteButton,
                        confirmDeleteButton
                    }
                };

                AddChild(internalHBox);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (!disposing)
                    return;

                IoCManager.Resolve<IEntityManager>().DeleteEntity(_previewDummy);
                _previewDummy = default;
            }
        }
    }
}
