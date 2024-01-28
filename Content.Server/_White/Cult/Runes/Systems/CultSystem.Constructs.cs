using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind.Components;
using Content.Shared._White.Cult.UI;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using ConstructShellComponent = Content.Shared._White.Cult.Components.ConstructShellComponent;

namespace Content.Server._White.Cult.Runes.Systems;

public partial class CultSystem
{
    [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public void InitializeConstructs()
    {
        SubscribeLocalEvent<ConstructShellComponent, ContainerIsInsertingAttemptEvent>(OnShardInsertAttempt);
        SubscribeLocalEvent<ConstructShellComponent, ComponentInit>(OnShellInit);
        SubscribeLocalEvent<ConstructShellComponent, ComponentRemove>(OnShellRemove);
        SubscribeLocalEvent<ConstructShellComponent, ConstructFormSelectedEvent>(OnShellSelected);
    }

    private void OnShellSelected(EntityUid uid, ConstructShellComponent component, ConstructFormSelectedEvent args)
    {
        var ent = args.Session.AttachedEntity;

        if (ent != null)
        {
            var construct = Spawn(args.SelectedForm, Transform(ent.Value).MapPosition);
            var mind = Comp<MindContainerComponent>(args.Session.AttachedEntity!.Value);

            if(!mind.HasMind)
                return;

            _mindSystem.TransferTo(mind.Mind.Value, construct);
        }

        Del(uid);
    }

    private void OnShellInit(EntityUid uid, ConstructShellComponent component, ComponentInit args)
    {
        _slotsSystem.AddItemSlot(uid, component.ShardSlotId, component.ShardSlot);
    }

    private void OnShellRemove(EntityUid uid, ConstructShellComponent component, ComponentRemove args)
    {
        _slotsSystem.RemoveItemSlot(uid, component.ShardSlot);
    }

    private void OnShardInsertAttempt(EntityUid uid, ConstructShellComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!TryComp<MindContainerComponent>(args.EntityUid, out var mindContainer) || !mindContainer.HasMind || !TryComp<ActorComponent>(args.EntityUid, out var actor))
        {
            _popupSystem.PopupEntity("Нет души", uid);
            args.Cancel();
            return;
        }

        _slotsSystem.SetLock(uid, component.ShardSlotId, true);
        _ui.TryOpen(uid, SelectConstructUi.Key, actor.PlayerSession);
    }
}
