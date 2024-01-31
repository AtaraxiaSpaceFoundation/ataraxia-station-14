using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Ohio.Buttons;

[Virtual]
public class OhioLobbyButton : BaseButton
{
    private Vector2 _scale = new(1, 1);

    private Texture? _texture;
    private Texture? _textureDefault;
    private Texture? _textureHighLighted;
    private Texture? _texturePressed;
    private Texture? _textureDisabled;

    private string? _texturePath;
    private string? _textureHighLightedPath;
    private string? _texturePressedPath;
    private string? _textureDisabledPath;

    private Texture? _arrowTexture;
    private string? _arrowTexturePath = "/Textures/Ohio/Lobby/arrow.png";

    public OhioLobbyButton()
    {
        _arrowTexture = Theme.ResolveTexture(_arrowTexturePath);

        DrawModeChanged();
    }

    // Textures GET-SET Start

    [ViewVariables]
    private Texture? TextureNormal
    {
        get => _texture;
        set
        {
            _texture = value;
            InvalidateMeasure();
        }
    }

    [ViewVariables]
    public Texture? TextureDefault
    {
        get => _textureDefault;
        set
        {
            _textureDefault = value;
            InvalidateMeasure();
        }
    }

    [ViewVariables]
    public Texture? TextureHighLighted
    {
        get => _textureHighLighted;
        set
        {
            _textureHighLighted = value;
            InvalidateMeasure();
        }
    }


    [ViewVariables]
    public Texture? TexturePressed
    {
        get => _texturePressed;
        set
        {
            _texturePressed = value;
            InvalidateMeasure();
        }
    }

    [ViewVariables]
    public Texture? TextureDisabled
    {
        get => _textureDisabled;
        set
        {
            _textureDisabled = value;
            InvalidateMeasure();
        }
    }

    // Textures GET-SET END

    // Textures Path GET-SET START

    public string TexturePath
    {
        set
        {
            _texturePath = value;
            TextureNormal = Theme.ResolveTexture(_texturePath);
            TextureDefault = TextureNormal;
        }
    }

    public string TextureHighLightedPath
    {
        set
        {
            _textureHighLightedPath = value;
            TextureHighLighted = Theme.ResolveTexture(_textureHighLightedPath);
        }
    }

    public string TexturePressedPath
    {
        set
        {
            _texturePressedPath = value;
            TexturePressed = Theme.ResolveTexture(_texturePressedPath);
        }
    }

    public string TextureDisabledPath
    {
        set
        {
            _textureDisabledPath = value;
            TextureDisabled = Theme.ResolveTexture(_textureDisabledPath);
        }
    }

    // Textures Path GET-SET END


    // Arrow Texture GET-SET START

    public Texture? ArrowTexture
    {
        get => _arrowTexture;
        set
        {
            _arrowTexture = value;
            InvalidateMeasure();
        }
    }

    public string ArrowTexturePath
    {
        set
        {
            _arrowTexturePath = value;
            ArrowTexture = Theme.ResolveTexture(_arrowTexturePath);
        }
    }

    // Arrow Texture GET-SET END

    public Vector2 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            InvalidateMeasure();
        }
    }

    protected override void DrawModeChanged()
    {
        _texture = DrawMode switch
        {
            DrawModeEnum.Normal => _textureDefault,
            DrawModeEnum.Pressed => _texturePressed,
            DrawModeEnum.Hover => _textureHighLighted,
            DrawModeEnum.Disabled => _textureDisabled,
            _ => _textureDefault
        };
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var texture = TextureNormal;

        if (texture == null)
            return;

        handle.DrawTextureRectRegion(texture, PixelSizeBox);

        // Draw the arrow indicator when selected
        if (IsHovered)
        {
            var arrowTexture = _arrowTexture;

            if (arrowTexture == null)
            {
                return;
            }

            var arrowPosition = new Vector2(SizeBox.Right - 150, SizeBox.Top + (SizeBox.Height - arrowTexture.Size.Y) / 2);

            handle.DrawTextureRectRegion(arrowTexture, UIBox2.FromDimensions(arrowPosition, arrowTexture.Size));
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var texture = TextureNormal;

        return Scale * (texture?.Size ?? Vector2.Zero);
    }
}
