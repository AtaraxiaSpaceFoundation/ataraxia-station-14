using System.Linq;
using System.Numerics;
using Content.Shared._White.Overlays;
using Content.Shared.Body.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client._White.Overlays;

public sealed class ThermalVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private readonly ContainerSystem _container;
    private readonly TransformSystem _transform;
    private readonly OccluderSystem _occluder;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly List<NightVisionRenderEntry> _entries = new();

    public ThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _transform = _entity.System<TransformSystem>();
        _occluder = _entity.System<OccluderSystem>();
        ZIndex = -1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_players.LocalEntity == null)
            return;

        var ent = _players.LocalEntity.Value;

        if ((!_entity.TryGetComponent(ent, out ThermalVisionComponent? component) || !component.IsActive) &&
            !_entity.HasComponent<TemporaryThermalVisionComponent>(ent))
        {
            return;
        }

        if (HasOccluders(ent))
            return;

        var handle = args.WorldHandle;
        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;

        _entries.Clear();
        var entities = _entity.EntityQueryEnumerator<BodyComponent, SpriteComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            if (HasOccluders(uid))
                continue;

            _entries.Add(new NightVisionRenderEntry((uid, sprite, xform),
                eye?.Position.MapId,
                eyeRot));
        }

        foreach (var entry in _entries)
        {
            Render(entry.Ent, entry.Map, handle, entry.EyeRot);
        }

        handle.SetTransform(Matrix3.Identity);
    }

    private void Render(Entity<SpriteComponent, TransformComponent> ent,
        MapId? map,
        DrawingHandleWorld handle,
        Angle eyeRot)
    {
        var (uid, sprite, xform) = ent;
        if (xform.MapID != map || _container.IsEntityOrParentInContainer(uid))
            return;

        var position = _transform.GetWorldPosition(xform);
        var rotation = _transform.GetWorldRotation(xform);

        sprite.Render(handle, eyeRot, rotation, position: position);
    }

    private bool HasOccluders(EntityUid ent)
    {
        var mapCoordinates = _transform.GetMapCoordinates(ent);
        var occluders = _occluder.QueryAabb(mapCoordinates.MapId,
            Box2.CenteredAround(mapCoordinates.Position, new Vector2(0.4f, 0.4f)));
        return occluders.Any(o => o.Component.Enabled);
    }
}

public record struct NightVisionRenderEntry(
    (EntityUid, SpriteComponent, TransformComponent) Ent,
    MapId? Map,
    Angle EyeRot);
