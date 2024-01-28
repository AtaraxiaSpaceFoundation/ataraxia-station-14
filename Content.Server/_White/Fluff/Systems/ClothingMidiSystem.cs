using System.Linq;
using Content.Server._White.Fluff.Components;
using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Robust.Server.GameObjects;

namespace Content.Server._White.Fluff.Systems;

public sealed class ClothingMidiSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingMidiComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<ClothingMidiComponent, GotUnequippedEvent>(OnUnequip);

    }

    private void OnEquip(EntityUid uid, ClothingMidiComponent component, GotEquippedEvent args)
    {
        if (component.MidiAction != null)
        {
            if (!TryComp<UserInterfaceComponent>(args.Equipment, out var ui) || ui.Interfaces.Count == 0)
                return;
            var comp = EnsureComp<UserInterfaceComponent>(args.Equipee);
            comp.Interfaces.Add(ui.Interfaces.First().Key, ui.Interfaces.First().Value);

            _actionsSystem.AddAction(args.Equipee, ref component.MidiActionEntity, component.MidiAction);
        }
    }

    private void OnUnequip(EntityUid uid, ClothingMidiComponent component, GotUnequippedEvent args)
    {
        if (component.MidiAction != null)
        {
            _actionsSystem.RemoveAction(args.Equipee, component.MidiActionEntity);

            if (!TryComp<UserInterfaceComponent>(args.Equipment, out var ui) || ui.Interfaces.Count == 0)
                return;

            if (!TryComp<UserInterfaceComponent>(args.Equipee, out var personUi))
                return;

            if (personUi.Interfaces.Count is 0 or 1)
            {
                RemComp<UserInterfaceComponent>(args.Equipee);
                return;
            }

            personUi.Interfaces.Remove(ui.Interfaces.First().Key);
        }
    }
}
