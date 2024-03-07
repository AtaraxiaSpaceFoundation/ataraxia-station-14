using Robust.Shared.GameStates;

namespace Content.Shared._White.Wizard.Magic;

[RegisterComponent, NetworkedComponent]
public sealed partial class MagicComponent : Component
{
    /// <summary>
    ///     Does this spell require Wizard Robes & Hat?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresClothes;
}
