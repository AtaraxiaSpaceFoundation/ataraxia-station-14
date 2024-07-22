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
    private readonly PointLightSystem _pointLight;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly List<NightVisionRenderEntry> _entries = new();
    private EntityUid _pointLightEntity;

    public ThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _transform = _entity.System<TransformSystem>();
        _occluder = _entity.System<OccluderSystem>();
        _pointLight = _entity.System<PointLightSystem>();
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

        var transform = _entity.GetComponent<TransformComponent>(ent);
        if (_pointLightEntity == default)
        {
            _pointLightEntity = _entity.SpawnAttachedTo(null, transform.Coordinates);
            var pointLight = _entity.EnsureComponent<PointLightComponent>(_pointLightEntity);
            _pointLight.SetRadius(_pointLightEntity, 3f, pointLight);
            _transform.SetParent(_pointLightEntity, ent);
        }
        else
        {
            var pointLightXForm = _entity.GetComponent<TransformComponent>(_pointLightEntity);
            if (pointLightXForm.ParentUid != ent)
                _transform.SetParent(_pointLightEntity, pointLightXForm, ent, transform);
            _transform.SetLocalPosition(_pointLightEntity, Vector2.Zero, pointLightXForm);
        }

        if (HasOccluders(ent))
            return;

        var handle = args.WorldHandle;
        var eye = args.Viewport.Eye;
        var mapId = eye?.Position.MapId;
        var eyeRot = eye?.Rotation ?? default;

        _entries.Clear();
        var entities = _entity.EntityQueryEnumerator<BodyComponent, SpriteComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            if (!CanSee(uid))
                continue;

            var entity = uid;

            if (_container.TryGetOuterContainer(uid, xform, out var container))
            {
                var owner = container.Owner;
                if (_entity.TryGetComponent<SpriteComponent>(owner, out var ownerSprite) &&
                    _entity.TryGetComponent<TransformComponent>(owner, out var ownerXform))
                {
                    entity = owner;
                    sprite = ownerSprite;
                    xform = ownerXform;
                }
            }

            if (_entries.Any(e => e.Ent.Item1 == entity))
                continue;

            _entries.Add(new NightVisionRenderEntry((entity, sprite, xform), mapId, eyeRot));
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
        if (xform.MapID != map || HasOccluders(uid) || !CanSee(uid))
            return;

        var position = _transform.GetWorldPosition(xform);
        var rotation = _transform.GetWorldRotation(xform);

        sprite.Render(handle, eyeRot, rotation, position: position);
    }

    private bool CanSee(EntityUid ent)
    {
        return !_entity.HasComponent<ThermalBlockerComponent>(ent);
    }

    private bool HasOccluders(EntityUid ent)
    {
        var mapCoordinates = _transform.GetMapCoordinates(ent);
        var occluders = _occluder.QueryAabb(mapCoordinates.MapId,
            Box2.CenteredAround(mapCoordinates.Position, new Vector2(0.4f, 0.4f)));
        return occluders.Any(o => o.Component.Enabled);
    }

    public void Reset()
    {
        if (_pointLightEntity == default)
            return;

        _entity.DeleteEntity(_pointLightEntity);
        _pointLightEntity = default;
    }
}

public record struct NightVisionRenderEntry(
    (EntityUid, SpriteComponent, TransformComponent) Ent,
    MapId? Map,
    Angle EyeRot);
