using Content.Shared.Movement.Systems;

namespace Content.Server._White.Other.FastAndFuriousSystem;

public sealed class FastAndFuriousSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FastAndFuriousComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FastAndFuriousComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<FastAndFuriousComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
    }

    private void OnMapInit(Entity<FastAndFuriousComponent> ent, ref MapInitEvent args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent);
    }
}
