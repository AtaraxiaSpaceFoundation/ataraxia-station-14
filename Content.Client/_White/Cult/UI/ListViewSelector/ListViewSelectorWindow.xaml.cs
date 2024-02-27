﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Cult.UI.ListViewSelector;

[GenerateTypedNameReferences]
public partial class ListViewSelectorWindow : DefaultWindow
{
    public Action<string, int>? ItemSelected;

    private readonly IPrototypeManager _prototypeManager;

    public ListViewSelectorWindow(IPrototypeManager prototypeManager)
    {
        RobustXamlLoader.Load(this);
        _prototypeManager = prototypeManager;
    }

    public void PopulateList(List<string> items, bool isPrototypes)
    {
        ItemsContainer.RemoveAllChildren();

        foreach (var item in items)
        {
            var button = new Button();
            var itemName = Loc.GetString($"ent-{item}");

            if (isPrototypes)
            {
                if (_prototypeManager.TryIndex<EntityPrototype>(item, out var itemPrototype))
                {
                    itemName = itemPrototype.Name;
                }
            }

            button.Text = itemName;

            button.OnPressed += _ => ItemSelected?.Invoke(item, items.IndexOf(item));

            ItemsContainer.AddChild(button);
        }
    }
}