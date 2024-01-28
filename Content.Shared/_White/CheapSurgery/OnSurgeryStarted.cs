using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.CheapSurgery;

[Serializable, NetSerializable]
public sealed class OnSurgeryStarted : EntityEventArgs
{
    public NetEntity Target;
    public List<OrganItem> OrganItems;

    public OnSurgeryStarted(NetEntity target, List<OrganItem> organItems)
    {
        Target = target;
        OrganItems = organItems;
    }
}

[Serializable, NetSerializable]
public sealed class OrganItem
{
    public string Name;
    public NetEntity Uid;
    public SpriteSpecifier Icon;
    public List<OrganItem> Children = new();

    public OrganItem(string name, NetEntity uid, SpriteSpecifier icon)
    {
        Name = name;
        Uid = uid;
        Icon = icon;
    }
}

[Serializable, NetSerializable]
public sealed class OnOrganSelected : EntityEventArgs
{
    public NetEntity Uid;

    public OnOrganSelected(NetEntity uid)
    {
        Uid = uid;
    }
}
