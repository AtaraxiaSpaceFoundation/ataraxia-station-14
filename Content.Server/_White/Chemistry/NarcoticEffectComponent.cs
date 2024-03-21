using System.Threading;

namespace Content.Server._White.Chemistry;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class NarcoticEffectComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float StunTime = 3f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public CancellationTokenSource cancelTokenSource = new();

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<string> Effects = new() { "Stun", "TremorAndShake", "Tremor", "Shake", "StunAndShake" };
}
