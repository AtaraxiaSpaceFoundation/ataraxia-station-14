using Content.Server.UserInterface;
using Content.Shared._White.Cult.UI;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Cult;

[RegisterComponent]
public sealed partial class RuneDrawerProviderComponent : Component
{
    [ViewVariables]
    public Enum UserInterfaceKey = ListViewSelectorUiKey.Key;

    [DataField("runePrototypes")]
    public List<EntProtoId> RunePrototypes = new();
}
