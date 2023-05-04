using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client.Preferences.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private readonly IPrototypeManager _prototypeManager;

        private void RandomizeEverything()
        {
            Profile = HumanoidCharacterProfile.Random();
            UpdateControls();
            IsDirty = true;
        }

        private void RandomizeName()
        {
            if (Profile == null) return;
            var name = HumanoidCharacterProfile.GetName(Profile.Species, Profile.Gender);
            SetName(name);
            UpdateNamesEdit();
        }

        private void RandomizeClownName()
        {
            if (Profile == null) return;
            var name = HumanoidCharacterProfile.GetClownName();
            SetClownName(name);
            UpdateNamesEdit();
        }

        private void RandomizeMimeName()
        {
            if (Profile == null) return;
            var name = HumanoidCharacterProfile.GetMimeName();
            SetMimeName(name);
            UpdateNamesEdit();
        }

        private void RandomizeBorgName()
        {
            if (Profile == null) return;
            var name = HumanoidCharacterProfile.GetBorgName();
            SetBorgName(name);
            UpdateNamesEdit();
        }
    }
}
