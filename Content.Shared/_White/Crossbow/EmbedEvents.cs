using Content.Shared.Projectiles;

namespace Content.Shared._White.Crossbow;

[ByRefEvent]
public readonly record struct EmbedStartEvent(EmbeddableProjectileComponent Embed);

public sealed class EmbedRemovedEvent : EntityEventArgs
{
}
