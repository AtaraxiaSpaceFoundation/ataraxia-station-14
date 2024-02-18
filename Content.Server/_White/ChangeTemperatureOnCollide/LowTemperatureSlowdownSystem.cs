using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server._White.ChangeTemperatureOnCollide;

public sealed class LowTemperatureSlowdownSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementSpeedModifierComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<TemperatureComponent, RefreshMovementSpeedModifiersEvent>(OnMoveSpeedRefresh);
    }

    private void OnMoveSpeedRefresh(EntityUid uid, TemperatureComponent component,
        RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = !component.Slowdown ? 1f : GetSpeedModifier(component.CurrentTemperature);
        args.ModifySpeed(modifier, modifier);
    }

    private void OnTemperatureChange(EntityUid uid, MovementSpeedModifierComponent component,
        OnTemperatureChangeEvent args)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if(GetSpeedModifier(args.LastTemperature) == GetSpeedModifier(args.CurrentTemperature))
            return;

        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid, component);
    }

    private static float GetSpeedModifier(float temperature)
    {
        return temperature switch
        {
            > 290f => 1f,
            > 280f => 0.9f,
            > 260f => 0.8f,
            > 230f => 0.7f,
            > 200f => 0.6f,
            > 160f => 0.5f,
            > 110f => 0.4f,
            > 50f => 0.3f,
            _ => 0.2f
        };
    }
}
