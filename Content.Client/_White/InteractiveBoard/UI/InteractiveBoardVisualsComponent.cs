using System.Numerics;

namespace Content.Client._White.InteractiveBoard.UI;

[RegisterComponent]
public sealed partial class InteractiveBoardVisualsComponent : Component
{
    public string ImagePath = "/Textures/White/Interface/InteractiveBoard/interactiveboardbackground.png";

    public Box2 BackgroundPatchMargin = default;

    public Color BackgroundModulate = Color.White;

    public bool BackgroundImageTile = false;

    public Vector2 BackgroundScale = Vector2.One;

    public string? HeaderImagePath;

    public Color HeaderImageModulate = Color.White;

    public Box2 HeaderMargin = default;

    public string? ContentImagePath;

    public Color ContentImageModulate = Color.White;

    public Box2 ContentMargin = default;

    public int ContentImageNumLines = 1;

    public Color FontAccentColor = new(223, 223, 213);

    public Vector2? MaxWritableArea = null;
}
