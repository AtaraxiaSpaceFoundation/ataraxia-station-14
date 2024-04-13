using Content.Shared._White.InteractiveBoard;
using Robust.Shared.GameStates;

namespace Content.Server._White.InteractiveBoard;

[NetworkedComponent, RegisterComponent]
public sealed partial class InteractiveBoardComponent : SharedInteractiveBoardComponent
{
    public InteractiveBoardAction Mode;

    public string Content { get; set; } = "";

    public int ContentSize { get; set; } = 6000;
}
