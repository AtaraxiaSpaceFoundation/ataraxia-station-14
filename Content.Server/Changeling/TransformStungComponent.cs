using Content.Shared.Changeling;

namespace Content.Server.Changeling;

[RegisterComponent]
public sealed partial class TransformStungComponent : Component
{
    [ViewVariables]
    public HumanoidData OriginalHumanoidData;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(10);

    [ViewVariables]
    public float Accumulator;
}
