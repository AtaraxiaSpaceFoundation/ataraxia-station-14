using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;


[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbedComponent : Component
{
    public EntityUid AbsorberMind;
}
