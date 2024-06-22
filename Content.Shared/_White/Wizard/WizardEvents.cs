using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Magic;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.Wizard;

#region HelperEvents

[Serializable, NetSerializable]
public sealed partial class ScrollDoAfterEvent : SimpleDoAfterEvent
{
}

[ByRefEvent]
public struct BeforeCastSpellEvent
{
    public EntityUid Performer;

    public bool Cancelled;

    public BeforeCastSpellEvent(EntityUid performer)
    {
        Performer = performer;
    }
}

[Serializable, NetSerializable]
public sealed partial class AddWizardChargeEvent : EntityEventArgs
{
    public string ChargeProto;

    public AddWizardChargeEvent(string chargeProto)
    {
        ChargeProto = chargeProto;
    }
}

[Serializable, NetSerializable]
public sealed partial class RemoveWizardChargeEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class RequestSpellChargingAudio : EntityEventArgs
{
    public SoundSpecifier Sound;
    public bool Loop;

    public RequestSpellChargingAudio(SoundSpecifier sound, bool loop)
    {
        Sound = sound;
        Loop = loop;
    }
}

[Serializable, NetSerializable]
public sealed partial class RequestSpellChargedAudio : EntityEventArgs
{
    public SoundSpecifier Sound;
    public bool Loop;

    public RequestSpellChargedAudio(SoundSpecifier sound, bool loop)
    {
        Sound = sound;
        Loop = loop;
    }
}

[Serializable, NetSerializable]
public sealed partial class RequestAudioSpellStop : EntityEventArgs
{
}

#endregion

#region Spells

public sealed partial class ArcSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("posData")]
    public MagicSpawnData Pos = new TargetCasterPos();

    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class ForceSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class FireballSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("posData")]
    public MagicSpawnData Pos = new TargetCasterPos();

    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class CardsSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("posData")]
    public MagicSpawnData Pos = new TargetCasterPos();

    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class ForceWallSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class BlinkSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class EtherealJauntSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class EmpSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class CluwneCurseSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class BananaTouchSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class MimeTouchSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class InstantRecallSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class TeleportSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class MindswapSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class StopTimeSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class ArcaneBarrageSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("speech")]
    public string? Speech { get; private set; }
}

#endregion
