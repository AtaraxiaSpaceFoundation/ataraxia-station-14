namespace Content.Server.Animations;

[RegisterComponent]
public sealed partial class DancingComponent : Component
{
    public float AccumulatedFrametime;

    public float NextDelay;
}
