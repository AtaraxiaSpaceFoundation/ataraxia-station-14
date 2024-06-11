using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Wizard.SpellBlade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpellBladeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public string ChosenAspect = string.Empty;

    [DataField]
    public List<EntProtoId> Aspects = new()
    {
        "AspectFire",
        "AspectFrost",
        "AspectLightning",
        "AspectBluespace",
        "AspectMagicMissile"
    };

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier AspectChosenSound = new SoundPathSpecifier("/Audio/White/Magic/spellblade-aspect.ogg");
}

[Serializable, NetSerializable]
public sealed class SpellBladeSystemMessage(EntProtoId protoId) : BoundUserInterfaceMessage
{
    public EntProtoId ProtoId = protoId;
}


[Serializable, NetSerializable]
public enum SpellBladeUiKey : byte
{
    Key
}
