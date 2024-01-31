using Robust.Shared.Serialization;

namespace Content.Shared.Miracle.UI;

[Serializable, NetSerializable]
public enum TransformStingSelectorUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class TransformStingBuiState : BoundUserInterfaceState
{
    public Dictionary<string, string> Items { get; set; }
    public NetEntity Target { get; set; }

    public TransformStingBuiState(Dictionary<string, string> items, NetEntity target)
    {
        Items = items;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class TransformStingItemSelectedMessage : BoundUserInterfaceMessage
{
    public string SelectedItem { get; private set; }
    public NetEntity Target { get; private set; }

    public TransformStingItemSelectedMessage(string selectedItem, NetEntity target)
    {
        SelectedItem = selectedItem;
        Target = target;
    }
}
