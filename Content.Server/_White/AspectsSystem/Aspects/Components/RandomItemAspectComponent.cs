namespace Content.Server._White.AspectsSystem.Aspects.Components;

[RegisterComponent]
public sealed partial class RandomItemAspectComponent : Component
{
    [ViewVariables]
    public string? Item;
}
