using Content.Shared.Hands;
using Content.Shared.Hands.Components;

namespace Content.Shared._White.Sounds;


public sealed class GotRemovedEvent : EquippedHandEvent
{
    public GotRemovedEvent(EntityUid user, EntityUid unequipped, Hand hand) : base(user, unequipped, hand) { }
}
