using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body;

public sealed class BodyRejuvenateSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnRejuvenate(Entity<BodyComponent> ent, ref RejuvenateEvent args)
    {
        if (ent.Comp.Prototype == null)
            return;

        var prototype = _prototypeManager.Index(ent.Comp.Prototype.Value);
        var protoSlots = prototype.Slots.Values.ToList();

        foreach (var (id, component) in _body.GetBodyChildren(ent.Owner, ent.Comp))
        {
            foreach (var organSlot in component.Organs.Values)
            {
                if (!_container.TryGetContainer(id, SharedBodySystem.GetOrganContainerId(organSlot.Id),
                        out var container))
                    continue;

                if (container.Count > 0)
                    continue;

                var organ = protoSlots.Where(x => x.Organs.ContainsKey(organSlot.Id))
                    .Select(x => x.Organs[organSlot.Id]).FirstOrDefault();

                TrySpawnInContainer(organ, id, SharedBodySystem.GetOrganContainerId(organSlot.Id), out _);
            }
        }
    }
}
