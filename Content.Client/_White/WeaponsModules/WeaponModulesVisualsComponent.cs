using Robust.Shared.GameStates;

namespace Content.Client._White.WeaponsModules;

/// <inheritdoc/>
[RegisterComponent, Access(typeof(WeaponModulesVisuals))]
public sealed partial class WeaponModulesVisualsComponent : Component
{
    [DataField()] public string? state;
}

public enum ModuleVisualState : byte
{
    Laser
}
