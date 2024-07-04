using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Ataraxia.Gasp
{
    [RegisterComponent]
    public sealed partial class GaspComponent : Component
    {


        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("GaspInterval")]
        public float GaspInterval = 5;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ThresholdGasp")]
        public FixedPoint2 ThresholdGasp = 6;





        [DataField("nextGaspTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan NextGaspTime = TimeSpan.FromSeconds(0);
    }
}
