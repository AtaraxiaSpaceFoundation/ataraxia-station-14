using Content.Shared.Rotation;
using Robust.Client.GameObjects;

namespace Content.Client._White.Rotation;

public sealed class RotationVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RotationVisualsComponent, MoveEvent>(OnMove);
    }

    private void OnMove(EntityUid uid, RotationVisualsComponent component, ref MoveEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state, appearance);

        var rotation = _transform.GetWorldRotation(uid);

        if (rotation.GetDir() is Direction.East or Direction.North or Direction.NorthEast or Direction.SouthEast)
        {
            if (state == RotationState.Horizontal &&
                sprite.Rotation == component.DefaultRotation)
            {
                sprite.Rotation = Angle.FromDegrees(270);
            }

            return;
        }
        if (state == RotationState.Horizontal &&
            sprite.Rotation == Angle.FromDegrees(270))
        {
            sprite.Rotation = component.DefaultRotation;
        }
    }
}
