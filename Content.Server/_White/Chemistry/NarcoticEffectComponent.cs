using System.Threading;

namespace Content.Server._White.Chemistry;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class NarcoticEffectComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float StunTime = 0.7f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public CancellationTokenSource cancelTokenSource = new();

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<string> Effects = new() { "Stun", "TremorAndShake", "Tremor", "Shake", "StunAndShake" };

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> TimerInterval = new() { 3000, 6000, 3800, 7000, 5000 };

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> SlurTime = new() { 35, 60, 80, 90, 45 };
}
