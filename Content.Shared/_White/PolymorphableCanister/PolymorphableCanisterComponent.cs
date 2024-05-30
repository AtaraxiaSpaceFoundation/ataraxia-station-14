using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.PolymorphableCanister;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PolymorphableCanisterComponent : Component
{
    [DataField]
    public ResPath ResPath = new("Structures/Storage/canister.rsi");

    [DataField, AutoNetworkedField]
    public ProtoId<EntityPrototype>? CurrentPrototype;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DoAfterTime = 3;

    [DataField]
    public List<ProtoId<EntityPrototype>> Prototypes = new()
    {
        "GasCanister",
        "StorageCanister",
        "AirCanister",
        "OxygenCanister",
        "NitrogenCanister",
        "CarbonDioxideCanister",
        "PlasmaCanister",
        "TritiumCanister",
        "WaterVaporCanister",
        "AmmoniaCanister",
        "NitrousOxideCanister",
        "FrezonCanister",
        "BZCanister",
        "PluoxiumCanister",
        "HydrogenCanister",
        "NitriumCanister",
        "HealiumCanister",
        "HyperNobliumCanister",
        "ProtoNitrateCanister",
        "ZaukerCanister",
        "HalonCanister",
        "HeliumCanister",
        "AntiNobliumCanister",
    };
}
