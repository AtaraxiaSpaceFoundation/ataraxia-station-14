using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Borer;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BorerHostComponent : Component
{
    //public EntityUid Borer;
    public Container BorerContainer;
}
