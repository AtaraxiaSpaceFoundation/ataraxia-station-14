namespace Content.Server.White.Cult;

[RegisterComponent]
public sealed partial class ConstructComponent : Component
{
    [DataField("actions")]
    public List<string> Actions = new();
}
