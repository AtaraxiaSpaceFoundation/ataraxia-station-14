using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing
{
    [RegisterComponent]
    public sealed partial class ClothingGrantComponentComponent : Component
    {
        [DataField("component", required: true)]
        [AlwaysPushInheritance]
        public ComponentRegistry Components { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsActive = false;
    }
}
