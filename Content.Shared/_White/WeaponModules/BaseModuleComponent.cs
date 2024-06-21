using Robust.Shared.GameStates;

namespace Content.Shared._White.WeaponModules;

[RegisterComponent, NetworkedComponent]
public partial class BaseModuleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("value")]
    public string AppearanceValue;

    [ViewVariables(VVAccess.ReadWrite), DataField("module_type")]
    public string ModuleType;
}
