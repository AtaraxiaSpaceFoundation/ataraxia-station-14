using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Ohio.UI;

public sealed class OhioRichTextLabel : RichTextLabel
{
    private Texture? _moonTexture;
    private string? _moonTexturePath = "/Textures/Ohio/Lobby/moon.png";

    public OhioRichTextLabel()
    {
        _moonTexture = Theme.ResolveTexture(_moonTexturePath);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var moonTexture = _moonTexture;

        if (moonTexture == null)
            return;

        var moonPosition = new Vector2(SizeBox.Right + 2, SizeBox.Top + (SizeBox.Height - moonTexture.Size.Y) / 2);

        handle.DrawTextureRectRegion(moonTexture, UIBox2.FromDimensions(moonPosition, moonTexture.Size));
    }
}
