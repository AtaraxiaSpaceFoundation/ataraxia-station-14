using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Cult.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConcealableComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Concealed;

    [DataField]
    public bool ExaminableWhileConcealed;

    [DataField]
    public bool IconSmooth;

    [DataField]
    public bool InteractionOutline;

    [DataField]
    public ResPath? ConcealedSprite;

    [DataField]
    public ResPath? RevealedSprite;

    [DataField]
    public bool ChangeMeta;

    [DataField]
    public string ConcealedName = string.Empty;

    [DataField]
    public string ConcealedDesc = string.Empty;

    [DataField]
    public string RevealedName = string.Empty;

    [DataField]
    public string RevealedDesc = string.Empty;
}

[Serializable, NetSerializable]
public enum ConcealableAppearance
{
    Concealed
}
