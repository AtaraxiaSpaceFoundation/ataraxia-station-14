using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public sealed partial class StartingGearPrototype : IPrototype
    {
        [DataField]
        public Dictionary<string, EntProtoId> Equipment = new();

        /// <summary>
        /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
        /// </summary>
        [DataField]
        public EntProtoId? InnerClothingSkirt;

        [DataField]
        public EntProtoId? Satchel;

        [DataField]
        public EntProtoId? Duffelbag;

        [DataField]
        public List<EntProtoId> Inhand = new(0);
        // White underwear
        [DataField("underweart")]
        private string _underweart = string.Empty;

        [DataField("underwearb")]
        private string _underwearb = string.Empty;
        // White underwear end

        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = string.Empty;

        public string GetGear(string slot, HumanoidCharacterProfile? profile)
        {
            if (profile != null)
            {
                if (slot == "jumpsuit" && profile.Clothing == ClothingPreference.Jumpskirt && !string.IsNullOrEmpty(InnerClothingSkirt))
                    return InnerClothingSkirt;
                if (slot == "back" && profile.Backpack == BackpackPreference.Satchel && !string.IsNullOrEmpty(Satchel))
                    return Satchel;
                if (slot == "back" && profile.Backpack == BackpackPreference.Duffelbag && !string.IsNullOrEmpty(Duffelbag))
                    return Duffelbag;
                // White underwear
                if (slot == "underweart" && profile.Sex == Sex.Female && !string.IsNullOrEmpty(_underweart))
                    return _underweart;
                if (slot == "underwearb" && profile.Sex == Sex.Female && !string.IsNullOrEmpty(_underwearb))
                    return _underwearb;
                // White underwear end
            }

            return Equipment.TryGetValue(slot, out var equipment) ? equipment : string.Empty;
        }
    }
}
