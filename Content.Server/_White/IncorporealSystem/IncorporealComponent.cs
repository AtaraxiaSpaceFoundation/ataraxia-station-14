namespace Content.Server._White.IncorporealSystem;

[RegisterComponent]
public sealed partial class IncorporealComponent : Component
{
    [DataField("movementSpeedBuff")]
    public float MovementSpeedBuff = 1.5f;
}
