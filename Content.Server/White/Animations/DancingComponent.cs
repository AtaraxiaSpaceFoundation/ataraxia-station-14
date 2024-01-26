namespace Content.Server.White.Animations;

[RegisterComponent]
public sealed partial class DancingComponent : Component
{
    public float AccumulatedFrametime;

    public float NextDelay;
}
