namespace Content.Shared._White.ReduceBlindness;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class ReduceBlindnessComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ReduceAmount { get; set; } = 1.5f;
}
