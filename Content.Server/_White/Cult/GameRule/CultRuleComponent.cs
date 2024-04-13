using Content.Server.GameTicking.Presets;
using Content.Shared._White.Cult.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Cult.GameRule;

[RegisterComponent]
public sealed partial class CultRuleComponent : Component
{
    public readonly SoundSpecifier GreetingsSound = new SoundPathSpecifier("/Audio/White/Cult/blood_cult_greeting.ogg");

    [DataField]
    public ProtoId<GamePresetPrototype> CultGamePresetPrototype = "Cult";

    [DataField]
    public ProtoId<EntityPrototype> ReaperPrototype = "ReaperConstruct";

    [ViewVariables(VVAccess.ReadOnly), DataField("tileId")]
    public string CultFloor = "CultFloor";

    [DataField]
    public Color EyeColor = Color.FromHex("#f80000");

    public ProtoId<ReagentPrototype> HolyWaterReagent = "Holywater";

    [DataField]
    public int ReadEyeThreshold = 5;

    [DataField]
    public int PentagramThreshold = 8;

    [DataField]
    public List<ProtoId<EntityPrototype>> StartingItems = new();

    [DataField]
    public ProtoId<AntagPrototype> CultistRolePrototype = "Cultist";

    /// <summary>
    ///     Players who played as an cultist at some point in the round.
    /// </summary>
    public Dictionary<string, string> CultistsCache = new();

    public EntityUid? CultTarget;

    public List<CultistComponent> CurrentCultists = new();

    public List<ConstructComponent> Constructs = new();

    public CultWinCondition WinCondition = CultWinCondition.Draw;
}

public enum CultWinCondition : byte
{
    Draw,
    Win,
    Failure,
}

public sealed class CultNarsieSummoned : EntityEventArgs;
