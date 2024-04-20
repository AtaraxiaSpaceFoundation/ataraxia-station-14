using Content.Shared.Examine;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Popups;

/// <summary>
/// Draws popup text, either in world or on screen.
/// </summary>
public sealed class PopupOverlay(
    IConfigurationManager configManager,
    IEntityManager entManager,
    ISharedPlayerManager playerMgr,
    IPrototypeManager protoManager,
    IUserInterfaceManager uiManager,
    PopupUIController controller,
    ExamineSystemShared examine,
    SharedTransformSystem transform,
    PopupSystem popup)
    : Overlay
{
    private readonly ShaderInstance _shader = protoManager.Index<ShaderPrototype>("unshaded").Instance();

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        args.DrawingHandle.SetTransform(Matrix3.Identity);
        args.DrawingHandle.UseShader(_shader);
        var scale = configManager.GetCVar(CVars.DisplayUIScale);

        if (scale == 0f)
            scale = uiManager.DefaultUIScale;

        DrawWorld(args.ScreenHandle, args, scale);

        args.DrawingHandle.UseShader(null);
    }

    private void DrawWorld(DrawingHandleScreen worldHandle, OverlayDrawArgs args, float scale)
    {
        if (popup.WorldLabels.Count == 0 || args.ViewportControl == null)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        var viewPos = new MapCoordinates(args.WorldAABB.Center, args.MapId);
        var ourEntity = playerMgr.LocalEntity;

        foreach (var popup1 in popup.WorldLabels)
        {
            var mapPos = popup1.InitialPos.ToMap(entManager, transform);

            if (mapPos.MapId != args.MapId)
                continue;

            var distance = (mapPos.Position - args.WorldBounds.Center).Length();

            // Should handle fade here too wyci.
            if (!args.WorldBounds.Contains(mapPos.Position) || !examine.InRangeUnOccluded(viewPos, mapPos, distance,
                    e => e == popup1.InitialPos.EntityId || e == ourEntity, entMan: entManager))
                continue;

            var pos = matrix.Transform(mapPos.Position);
            controller.DrawPopup(popup1, worldHandle, pos, scale);
        }
    }
}
