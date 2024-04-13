using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.WeaponModules;

/// <summary>
/// Base Module Component
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public partial class WeaponModulesComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public List<EntityUid> Modules = new();

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool WeaponFireEffect;
}

[Serializable, NetSerializable]
public enum ModuleVisualState : byte
{
    BarrelModule,
    HandGuardModule
}

[Serializable, NetSerializable]
public enum Modules : byte
{
    Light
}
