using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TwoModeEnergyAmmoProviderComponent : BatteryAmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadOnly),
     DataField("stunPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string StunPrototype = default!;

    [ViewVariables(VVAccess.ReadOnly),
     DataField("laserPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string LaserPrototype = default!;

    [ViewVariables(VVAccess.ReadOnly), DataField("stunFireCost")]
    public float StunFireCost = 142;

    [ViewVariables(VVAccess.ReadOnly), DataField("laserFireCost")]
    public float LaserFireCost = 65;

    [ViewVariables(VVAccess.ReadOnly), DataField("stunProjectileSpeed")]
    public float StunProjectileSpeed = 12;

    [ViewVariables(VVAccess.ReadOnly), DataField("laserProjectileSpeed")]
    public float LaserProjectileSpeed = 25;

    [ViewVariables(VVAccess.ReadOnly), DataField("currentMode")]
    public EnergyModes CurrentMode { get; set; } = EnergyModes.Stun;

    [ViewVariables(VVAccess.ReadOnly), DataField("stunSound")]
    public SoundSpecifier? StunSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/taser2.ogg");

    [ViewVariables(VVAccess.ReadOnly), DataField("laserSound")]
    public SoundSpecifier? LaserSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser_cannon.ogg");

    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/egun_toggle.ogg");

    [ViewVariables(VVAccess.ReadOnly)] public bool InStun = true;
}

public enum EnergyModes
{
    Stun,
    Laser
}
