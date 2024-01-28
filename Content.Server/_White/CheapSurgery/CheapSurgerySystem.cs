using Content.Server.Body.Systems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Kitchen.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared._White.CheapSurgery;
using Robust.Shared.Utility;

namespace Content.Server._White.CheapSurgery;

public sealed class CheapSurgerySystem : SharedCheapSurgerySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ConstructionSystem _construction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, InteractUsingEvent>(OnUsing);
        SubscribeNetworkEvent<OnOrganSelected>(OnSelected);
    }

    private void OnSelected(OnOrganSelected ev)
    {
        var entity = GetEntity(ev.Uid);

        if (TryComp<BodyPartComponent>(entity, out var partComponent) && partComponent.Body != null)
        {
            StartDrop(partComponent.Body.Value, entity);
        }
        else if (TryComp<OrganComponent>(entity, out var organComponent) && organComponent.Body != null)
        {
            StartDrop(organComponent.Body.Value, entity);
        }
    }

    private void OnUsing(EntityUid uid, BodyComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<SharpComponent>(args.Used, out _) || _mobState.IsAlive(uid)
            || TryComp<ActiveSurgeryComponent>(uid, out _))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out _))
            return;

        var organs = GenList(uid);

        var ev = new OnSurgeryStarted(GetNetEntity(uid), organs);
        RaiseNetworkEvent(ev, args.User);
    }

    private OrganItem GetOrganItem(EntityUid part, List<OrganItem>? child = null)
    {
        var metadata = MetaData(part);

        var organ = new OrganItem(metadata.EntityName, GetNetEntity(part),
            new SpriteSpecifier.EntityPrototype(metadata.EntityPrototype!.ID));

        if (child != null)
            organ.Children = child;

        return organ;
    }

    public List<OrganItem> GenList(EntityUid uid)
    {
        var organs = new List<OrganItem>();

        if (TryComp<BodyComponent>(uid, out var bodyComponent))
        {
            foreach (var (part, _) in _body.GetBodyChildren(uid, bodyComponent))
            {
                if (part == uid)
                {
                    continue;
                }

                var child = GenList(part);
                if (child.Count > 0)
                    organs.Add(GetOrganItem(part, child));
            }
        }
        else if (TryComp<BodyPartComponent>(uid, out var partComponent))
        {
            foreach (var (part, _) in _body.GetBodyPartChildren(uid, partComponent))
            {
                if (part == uid)
                {
                    continue;
                }

                var child = GenList(part);
                if (child.Count > 0)
                    organs.Add(GetOrganItem(part, child));
            }

            foreach (var (part, _) in _body.GetPartOrgans(uid, partComponent))
            {
                organs.Add(GetOrganItem(part, GenList(part)));
            }
        }

        return organs;
    }

    public bool StartDrop(EntityUid uid, EntityUid organUid, EntityUid? user = null, BodyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        EnsureComp<ActiveSurgeryComponent>(uid).OrganUid = organUid;

        var construct = EnsureComp<ConstructionComponent>(uid);
        return _construction.ChangeGraph(uid, user, "BodySurgery", "head", true, construct);
    }
}
