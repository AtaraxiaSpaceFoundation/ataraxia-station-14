using Content.Client._White.InteractiveBoard.UI;
using Robust.Client.GameObjects;

using static Content.Shared._White.InteractiveBoard.SharedInteractiveBoardComponent;

namespace Content.Client._White.InteractiveBoard;

public sealed class InteractiveBoardSystem : VisualizerSystem<InteractiveBoardVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, InteractiveBoardVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<InteractiveBoardStatus>(uid, InteractiveBoardVisuals.Status , out var writingStatus, args.Component))
            args.Sprite.LayerSetVisible(InteractiveBoardVisualLayers.Writing, writingStatus == InteractiveBoardStatus.Written);
    }
}

public enum InteractiveBoardVisualLayers
{
    Writing
}

