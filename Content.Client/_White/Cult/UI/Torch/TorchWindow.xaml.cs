﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Cult.UI.Torch;

[GenerateTypedNameReferences]
public partial class TorchWindow : DefaultWindow
{
    public Action<string, string>? ItemSelected;

    public TorchWindow()
    {
        RobustXamlLoader.Load(this);
    }

    public void PopulateList(Dictionary<string, string> items)
    {
        ItemsContainer.RemoveAllChildren();

        foreach (var item in items.Keys)
        {
            var button = new Button();
            var itemName = items[item];

            button.Text = itemName;

            button.OnPressed += _ => ItemSelected?.Invoke(item, items[item]);

            ItemsContainer.AddChild(button);
        }
    }
}
