using Robust.Shared.GameStates;

namespace Content.Shared._White.Pinpointer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiplePinpointerComponent : Component
{
    [DataField(required: true)]
    public string[] Modes = Array.Empty<string>();

    [ViewVariables, AutoNetworkedField]
    public uint CurrentEntry = 0;
}
