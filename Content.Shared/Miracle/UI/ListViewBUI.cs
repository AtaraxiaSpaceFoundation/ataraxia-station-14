using Robust.Shared.Serialization;

namespace Content.Shared.Miracle.UI;

[Serializable, NetSerializable]
public enum ListViewSelectorUiKeyChangeling
{
    Key
}

[Serializable, NetSerializable]
public sealed class ListViewBuiState : BoundUserInterfaceState
{
    public Dictionary<string, string> Items { get; set; }

    public ListViewBuiState(Dictionary<string, string> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public sealed class ListViewItemSelectedMessage : BoundUserInterfaceMessage
{
    public string SelectedItem { get; private set; }

    public ListViewItemSelectedMessage(string selectedItem)
    {
        SelectedItem = selectedItem;
    }
}
