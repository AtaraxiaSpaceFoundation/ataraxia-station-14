using System.Numerics;
using Content.Shared.Borer;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Borer;

public sealed class BorerOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    [Dependency] private readonly IResourceCache _client = default!;
    private IPlayerManager _playerManager;
    private IEntityManager _entManager;
    private Font _font;
    private Texture _chemBackground;

    int points;
    float X, Y;

    public BorerOverlay(IEntityManager entManager, IPlayerManager playerManager, IResourceCache client)
    {
        _entManager = entManager;
        _playerManager = playerManager;
        _client = client;
        _font = new VectorFont(_client.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 30);
        _chemBackground = _client.GetResource<TextureResource>("/Textures/Interface/Borer/chem_bg.png");
    }
    protected override void Draw(in OverlayDrawArgs args)
    {
        var localPlayer = _playerManager.LocalEntity;
        if (_entManager.TryGetComponent(localPlayer, out BorerComponent? borComp))
            points = borComp.Points;
        else if (_entManager.TryGetComponent(localPlayer, out InfestedBorerComponent? infestedComp))
        {
            if (infestedComp.ControllingBrain)
                return;
            points = infestedComp.Points;
        }
        else
            return;

        if (args.ViewportControl != null)
        {
            X = (args.ViewportControl.Window!.Size.X / 2f) - 32;
            Y = args.ViewportControl.Window!.Size.Y - 130f;
        }

        var screenHandle = args.ScreenHandle;

        screenHandle.DrawTextureRect(_chemBackground, new UIBox2(new Vector2(X,Y), new Vector2(X+128f,Y+128f)));
        screenHandle.DrawString(_font, new Vector2(X+18, Y+42), points.ToString(), new Color(30, 200, 30));
    }
}
