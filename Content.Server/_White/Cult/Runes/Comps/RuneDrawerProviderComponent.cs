using Content.Server.UserInterface;
using Content.Shared._White.Cult.UI;
using Robust.Server.GameObjects;

namespace Content.Shared._White.Cult;

[RegisterComponent]
public sealed partial class RuneDrawerProviderComponent : Component
{
    [ViewVariables]
    public Enum UserInterfaceKey = ListViewSelectorUiKey.Key;

    [DataField("runePrototypes")]
    public List<string> RunePrototypes = new();
}
