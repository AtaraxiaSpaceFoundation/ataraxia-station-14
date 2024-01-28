namespace Content.Server._White.Animations;

[RegisterComponent]
public sealed partial class DancingComponent : Component
{
    public float AccumulatedFrametime;

    public float NextDelay;
}
