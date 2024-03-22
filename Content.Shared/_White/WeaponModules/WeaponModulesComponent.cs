﻿using Robust.Shared.GameStates;
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

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool UseEffect;
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
