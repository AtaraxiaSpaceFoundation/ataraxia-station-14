using System.Linq;
using Content.Client.IconSmoothing;
using Content.Client.Interactable.Components;
using Content.Shared._White.Cult.Components;
using Robust.Client.GameObjects;

namespace Content.Client._White.Cult.Concealable;

public sealed class ConcealableVisualizer : VisualizerSystem<ConcealableComponent>
{
    [Dependency] private readonly IconSmoothSystem _smooth = default!;

    protected override void OnAppearanceChange(EntityUid uid, ConcealableComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, ConcealableAppearance.Concealed, out var concealed, args.Component))
            return;

        if (component.IconSmooth)
            _smooth.SetEnabled(uid, concealed);

        if (component.InteractionOutline)
        {
            if (concealed)
            {
                if (TryComp(uid, out InteractionOutlineComponent? outline))
                {
                    outline.OnMouseLeave(uid);
                    RemComp<InteractionOutlineComponent>(uid);
                }
            }
            else
                EnsureComp<InteractionOutlineComponent>(uid);
        }

        if (concealed)
        {
            if (component.ConcealedSprite != null)
            {
                for (var i = 0; i < args.Sprite.AllLayers.Count(); i++)
                {
                    args.Sprite.LayerSetRSI(i, component.ConcealedSprite.Value);
                }
                return;
            }

            args.Sprite.Color = args.Sprite.Color.WithAlpha(0f);
        }
        else
        {
            if (component.RevealedSprite != null)
            {
                for (var i = 0; i < args.Sprite.AllLayers.Count(); i++)
                {
                    args.Sprite.LayerSetRSI(i, component.RevealedSprite.Value);
                }
                return;
            }

            args.Sprite.Color = args.Sprite.Color.WithAlpha(1f);
        }
    }
}
