using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Cult.UI;

[Serializable, NetSerializable]
public sealed class ApocalypseRuneDrawMessage : EuiMessageBase
{
    public readonly bool Accepted;

    public ApocalypseRuneDrawMessage(bool accepted)
    {
        Accepted = accepted;
    }
}
