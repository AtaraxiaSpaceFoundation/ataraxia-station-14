using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Wizard.Timestop;

[RegisterComponent, NetworkedComponent]
public sealed partial class FrozenComponent : Component
{
    [ViewVariables]
    public float Lifetime = 10f;

    [ViewVariables]
    public Vector2 OldLinearVelocity;

    [ViewVariables]
    public float OldAngularVelocity;
}
