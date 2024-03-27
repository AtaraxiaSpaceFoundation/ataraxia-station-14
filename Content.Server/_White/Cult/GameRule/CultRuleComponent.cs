using Content.Server.GameTicking.Presets;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared._White.Cult;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.GameRule;

[RegisterComponent]
public sealed partial class CultRuleComponent : Component
{
    public readonly SoundSpecifier GreetingsSound = new SoundPathSpecifier("/Audio/White/Cult/blood_cult_greeting.ogg");

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<GamePresetPrototype>))]
    public string CultGamePresetPrototype = "Cult";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string CultistPrototypeId = "Cultist";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ReaperPrototype = "ReaperConstruct";

    [ViewVariables(VVAccess.ReadOnly), DataField("tileId")]
    public string CultFloor = "CultFloor";

    [DataField]
    public Color EyeColor = Color.FromHex("#f80000");

    public string HolyWaterReagent = "Holywater";

    [DataField]
    public int ReadEyeThreshold = 5;

    [DataField]
    public int PentagramThreshold = 8;

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingItems = [];

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string CultistRolePrototype = "Cultist";

    /// <summary>
    ///     Players who played as an cultist at some point in the round.
    /// </summary>
    public Dictionary<string, string> CultistsCache = new();

    public EntityUid? CultTarget;

    public List<CultistComponent> CurrentCultists = [];

    public List<ConstructComponent> Constructs = [];

    public CultWinCondition WinCondition;
}

public enum CultWinCondition : byte
{
    Win,
    Failure
}

public sealed class CultNarsieSummoned : EntityEventArgs;
