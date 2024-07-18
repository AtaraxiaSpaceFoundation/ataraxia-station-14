using Content.Shared.Rotation;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._White.Rotation;

public sealed class RotationVisualizerWhiteSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RotationVisualsComponent, MoveEvent>(OnMove);
    }

    private void OnMove(EntityUid uid, RotationVisualsComponent component, ref MoveEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryComp<AppearanceComponent>(uid, out var appearance) ||
            !TryComp<TransformComponent>(uid, out var transform) ||
            component.DefaultRotation == 0)
            return;

        _appearance.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state, appearance);

        var rotation = transform.LocalRotation + _eyeManager.CurrentEye.Rotation;

        if (rotation.GetDir() is Direction.East or Direction.North or Direction.NorthEast or Direction.SouthEast)
        {
            if (state != RotationState.Horizontal ||
                sprite.Rotation != component.DefaultRotation)
                return;

            component.HorizontalRotation = Angle.FromDegrees(270);
            sprite.Rotation = Angle.FromDegrees(270);
            return;
        }

        if (state != RotationState.Horizontal ||
            sprite.Rotation != Angle.FromDegrees(270))
            return;

        component.HorizontalRotation = component.DefaultRotation;
        sprite.Rotation = component.DefaultRotation;
    }
}
