using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Client._White.WeaponsModules;

/// <inheritdoc/>
[RegisterComponent]
public sealed partial class WeaponModulesVisualsComponent : Component
{
    [DataField] public string? state;
}

[Serializable, NetSerializable]
public enum ModuleVisualState : byte
{
    Module
}

[Serializable, NetSerializable]
public enum Modules : byte
{
    Light
}
