using Robust.Shared.GameStates;

namespace Content.Shared.Borer;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class InfestedBorerComponent : Component
{
    [DataField("reproduceCost")]
    public int ReproduceCost = 100;

    [DataField("assumeControlCost")]
    public int AssumeControlCost = 250;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool ControllingBrain = false;

    public TimeSpan PointUpdateNext = TimeSpan.Zero;

    public TimeSpan PointUpdateRate = TimeSpan.FromSeconds(2);

    public readonly int PointUpdateValue = 1;

    public string ActionBorerOut = "ActionBorerOut";

    public EntityUid? ActionBorerOutEntity;

    public string ActionBorerBrainSpeech = "ActionBorerBrainSpeech";

    public EntityUid? ActionBorerBrainSpeechEntity;

    public string ActionBorerInjectWindowOpen = "ActionBorerInjectWindowOpen";

    public EntityUid? ActionBorerInjectWindowOpenEntity;

    public string ActionBorerScan = "ActionBorerScan";

    public EntityUid? ActionBorerScanEntity;

    public string ActionBorerBrainTake = "ActionBorerBrainTake";

    public EntityUid? ActionBorerBrainTakeEntity;

    public string ActionBorerBrainRelease = "ActionBorerBrainRelease";

    public EntityUid? ActionBorerBrainReleaseEntity;

    public string ActionBorerBrainResist = "ActionBorerBrainResist";

    public EntityUid? ActionBorerBrainResistEntity;

    public string ActionBorerReproduce = "ActionBorerReproduce";

    public EntityUid? ActionBorerReproduceEntity;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Host;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int Points = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<string, int> AvailableReagents = new()
    {
        { "Epinephrine", 30 },
        { "Bicaridine", 30 },
        { "Kelotane", 30 },
        { "Dylovene", 30 },
        { "Dexalin", 30 },
        { "SpaceDrugs", 75 },
        { "Leporazine", 75 }
    };
}
