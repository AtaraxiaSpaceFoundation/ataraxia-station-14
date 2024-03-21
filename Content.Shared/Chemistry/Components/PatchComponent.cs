using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

[Serializable, NetSerializable]
public sealed partial class PatchDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Implements draw/inject behavior for droppers and syringes.
/// </summary>
/// <remarks>
/// Can optionally support both
/// injection and drawing or just injection. Can inject/draw reagents from solution
/// containers, and can directly inject into a mobs bloodstream.
/// </remarks>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PatchComponent : Component
{

    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CurrentVolume;

    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 TotalVolume;

    [DataField("solutionName")]
    public string SolutionName = "patch";

    [DataField("onlyMobs")]
    public bool OnlyMobs = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

}
