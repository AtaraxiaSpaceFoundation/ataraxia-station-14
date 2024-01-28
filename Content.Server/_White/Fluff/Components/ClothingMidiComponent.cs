using Robust.Shared.Prototypes;

namespace Content.Server._White.Fluff.Components;

[RegisterComponent]
public sealed partial class ClothingMidiComponent : Component
{
    [DataField("midiAction", required: true, serverOnly: true)] // server only, as it uses a server-BUI event !type
    public EntProtoId? MidiAction;

    [DataField]
    public EntityUid? MidiActionEntity;
}
