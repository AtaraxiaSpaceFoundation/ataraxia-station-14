using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._White.Cult;

public sealed class HellSystem : EntitySystem
{
    private const string Rsi = "White/Cult/hell.rsi";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HellComponent, ComponentStartup>(PentagramAdded);
        SubscribeLocalEvent<HellComponent, ComponentShutdown>(PentagramRemoved);
    }

    private void PentagramAdded(EntityUid uid, HellComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet(HellKey.Key, out _))
            return;

        var adj = sprite.Bounds.Height / 2 + 1.0f/32 * 6.0f;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath(Rsi), "animated"));

        sprite.LayerMapSet(HellKey.Key, layer);
        sprite.LayerSetOffset(layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");
    }

    private void PentagramRemoved(EntityUid uid, HellComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(HellKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    private enum HellKey
    {
        Key
    }
}
