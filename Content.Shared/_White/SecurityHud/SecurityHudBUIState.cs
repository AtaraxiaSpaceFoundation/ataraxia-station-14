using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Shared._White.SecurityHud;

[Serializable, NetSerializable]
public enum SecurityHudUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SecurityHudBUIState : BoundUserInterfaceState
{
    public IReadOnlyCollection<string> Ids { get; set; }

    public NetEntity User { get; set; }

    public NetEntity Target { get; private set; }

    public SecurityHudBUIState(IReadOnlyCollection<string> ids, NetEntity user, NetEntity target)
    {
        Ids = ids;
        User = user;
        Target = target;
    }
}

[Serializable, NetSerializable]
public class SecurityHudStatusSelectedMessage : BoundUserInterfaceMessage
{
    public SecurityStatus Status { get; private set; }

    public NetEntity User { get; private set; }

    public NetEntity Target { get; private set; }

    public SecurityHudStatusSelectedMessage(SecurityStatus status, NetEntity user, NetEntity target)
    {
        Status = status;
        User = user;
        Target = target;
    }
}
