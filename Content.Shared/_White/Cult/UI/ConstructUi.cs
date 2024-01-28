using Robust.Shared.Serialization;

namespace Content.Shared._White.Cult.UI;

[Serializable, NetSerializable]
public enum SelectConstructUi
{
    Key
}

[Serializable, NetSerializable]
public class ConstructFormSelectedEvent : BoundUserInterfaceMessage
{
    public string SelectedForm;
    public ConstructFormSelectedEvent(string form)
    {
        SelectedForm = form;
    }
}
