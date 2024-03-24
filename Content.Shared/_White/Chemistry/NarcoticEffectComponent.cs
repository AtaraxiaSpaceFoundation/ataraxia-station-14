using System.Threading;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Chemistry;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class NarcoticEffectComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public CancellationTokenSource CancelTokenSource = new();

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> TimerInterval = new() { 3000, 6000, 3800, 7000, 5000 };

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> SlurTime = new() { 35, 60, 80, 90, 45 };
}

[Serializable, NetSerializable]
public enum NarcoticEffects
{
    Stun,
    Tremor,
    Shake,
    TremorAndShake,
    StunAndShake
}
