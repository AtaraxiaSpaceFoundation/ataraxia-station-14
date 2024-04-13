using Content.Server.Actions;
using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Changeling;

public sealed class HiveHeadSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiveHeadComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HiveHeadComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HiveHeadComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HiveHeadComponent, ReleaseBeesEvent>(OnReleaseBees);
    }

    private void OnReleaseBees(Entity<HiveHeadComponent> ent, ref ReleaseBeesEvent args)
    {
        if (!_mobState.IsAlive(args.Performer))
            return;

        args.Handled = true;

        var coords = Transform(args.Performer).Coordinates;

        for (var i = 0; i < ent.Comp.BeesAmount; i++)
        {
            Spawn(ent.Comp.BeeProto, coords);
        }
    }

    private void OnGetActions(Entity<HiveHeadComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags != SlotFlags.HEAD)
            return;

        args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
        _actions.SetCooldown(ent.Comp.ActionEntity, TimeSpan.FromSeconds(5));
    }

    private void OnShutdown(Entity<HiveHeadComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent, ent.Comp.ActionEntity);
    }

    private void OnMapInit(Entity<HiveHeadComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }
}
