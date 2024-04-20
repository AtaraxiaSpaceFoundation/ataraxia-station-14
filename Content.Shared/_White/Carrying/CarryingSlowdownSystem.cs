using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Carrying;

public sealed class CarryingSlowdownSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarryingSlowdownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    }

    public void SetModifier(EntityUid uid, CarryingSlowdownComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMoveSpeed(
        EntityUid uid,
        CarryingSlowdownComponent component,
        RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkModifier, component.SprintModifier);
    }
}

[Serializable, NetSerializable]
public sealed partial class CarryDoAfterEvent : SimpleDoAfterEvent;