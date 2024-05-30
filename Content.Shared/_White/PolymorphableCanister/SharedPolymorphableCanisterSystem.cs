using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.PolymorphableCanister;

public abstract class SharedPolymorphableCanisterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PolymorphableCanisterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PolymorphableCanisterComponent, PolymorphableCanisterMessage>(OnMessage);
        SubscribeLocalEvent<PolymorphableCanisterComponent, PolymorphableCanisterDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(Entity<PolymorphableCanisterComponent> ent, ref ComponentInit args)
    {
        var proto = MetaData(ent.Owner).EntityPrototype;
        if (proto is null)
        {
            return;
        }

        ent.Comp.CurrentPrototype = proto.ID;
        Dirty(ent);
    }

    private void OnMessage(Entity<PolymorphableCanisterComponent> ent, ref PolymorphableCanisterMessage args)
    {
        if (!args.Session.AttachedEntity.HasValue)
        {
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Session.AttachedEntity.Value,
            ent.Comp.DoAfterTime,
            new PolymorphableCanisterDoAfterEvent(args.ProtoId),
            ent.Owner
        )
        {
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<PolymorphableCanisterComponent> ent, ref PolymorphableCanisterDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            return;
        }

        ent.Comp.CurrentPrototype = args.ProtoId;
        UpdateAppearance(ent, args.ProtoId);
        Dirty(ent);
    }

    public void UpdateAppearance(EntityUid uid, ProtoId<EntityPrototype>? protoId)
    {
        if (string.IsNullOrEmpty(protoId) || !_proto.TryIndex(protoId, out var proto))
        {
            return;
        }

        var metadata = MetaData(uid);
        _metaData.SetEntityName(uid, proto.Name, metadata);
        _metaData.SetEntityDescription(uid, proto.Description, metadata);

        UpdateSprite(uid, proto);
    }

    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto)
    {
    }
}

[NetSerializable, Serializable]
public enum PolymorphableCanisterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class PolymorphableCanisterMessage(string protoId) : BoundUserInterfaceMessage
{
    public readonly ProtoId<EntityPrototype> ProtoId = protoId;
}

[Serializable, NetSerializable]
public sealed partial class PolymorphableCanisterDoAfterEvent : SimpleDoAfterEvent
{
    public readonly ProtoId<EntityPrototype> ProtoId;

    public PolymorphableCanisterDoAfterEvent(string protoId)
    {
        ProtoId = protoId;
    }
}