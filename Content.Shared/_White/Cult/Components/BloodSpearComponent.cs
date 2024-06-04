using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Cult.Components;

[RegisterComponent]
public sealed partial class BloodSpearComponent : Component
{
    [ViewVariables]
    public Entity<CultistComponent>? User;

    [DataField(required: true)]
    public EntProtoId Action;

    [DataField]
    public SoundSpecifier ShatterSound = new SoundCollectionSpecifier("GlassBreak");
}
