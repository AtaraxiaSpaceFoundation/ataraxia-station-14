using Content.Shared.Construction;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.Construction;

[UsedImplicitly, DataDefinition]
public sealed partial class AddTag : IGraphAction
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string Tag = default!;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        entityManager.System<TagSystem>().AddTag(uid, Tag);
    }
}
