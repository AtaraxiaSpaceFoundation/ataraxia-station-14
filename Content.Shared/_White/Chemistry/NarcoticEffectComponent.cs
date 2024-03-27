using System.Threading;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Chemistry;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class NarcoticEffectComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> TimerInterval = new() { 8000, 12000, 10000, 12500, 10500 };

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> SlurTime = new() { 35, 60, 80, 90, 45 };
}

[Serializable, NetSerializable]
public enum NarcoticEffects
{
    LieDown,
    Shake,
    LieDownAndShake
}
