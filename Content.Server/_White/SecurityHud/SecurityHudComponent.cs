using Content.Shared.Radio;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._White.SecurityHud;

[RegisterComponent]
public sealed partial class SecurityHudComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("criminalrecords", customTypeSerializer: typeof(PrototypeIdListSerializer<StatusIconPrototype>))]
    public IReadOnlyCollection<string> Status = ArraySegment<string>.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<RadioChannelPrototype> SecurityChannel = "Security";

    [ViewVariables(VVAccess.ReadOnly)]
    public string Reason = "Изменено с помощью визора";
}
