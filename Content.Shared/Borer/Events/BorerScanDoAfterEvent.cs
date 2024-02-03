using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Borer;

[Serializable, NetSerializable]
public sealed partial class BorerScanDoAfterEvent : EntityEventArgs
{
    public Dictionary<string, FixedPoint2> Solution;

    public BorerScanDoAfterEvent(Dictionary<string, FixedPoint2> solution)
    {
        Solution = solution;
    }
}
