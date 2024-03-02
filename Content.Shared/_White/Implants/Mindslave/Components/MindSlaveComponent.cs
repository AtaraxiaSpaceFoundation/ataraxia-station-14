using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.Implants.Mindslave.Components;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class MindSlaveComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<NetEntity> Slaves = new();

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public NetEntity Master;

    [DataField("slaveStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string SlaveStatusIcon = "SlaveMindslaveIcon";

    [DataField("masterStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string MasterStatusIcon = "MasterMindslaveIcon";
}
