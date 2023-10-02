using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.White.Economy.Ui;

[GenerateTypedNameReferences]
public sealed partial class VendingItem : Control
{
    public VendingItem(string itemName, string price, Texture? texture = null)
    {
        RobustXamlLoader.Load(this);

        VendingItemName.Text = itemName;

        VendingItemBuyButton.Text = price;
        // VendingItemBuyButton.Disabled = !canBuy;

        VendingItemTexture.Texture = texture;
    }
}
