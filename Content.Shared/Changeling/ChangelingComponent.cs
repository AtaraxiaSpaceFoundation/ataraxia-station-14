using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;


[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingComponent : Component
{
    [DataField("chemRegenRate")]
    public int ChemicalRegenRate = 2;

    [DataField("chemicalCap")]
    public int ChemicalCapacity = 75;

    [ViewVariables(VVAccess.ReadWrite), DataField("chemicalsBalance")]
    public int ChemicalsBalance = 20;

    [ViewVariables(VVAccess.ReadWrite), DataField("pointsBalance")]
    public int StartingPointsBalance = 10;

    [ViewVariables(VVAccess.ReadOnly)]
    public float Accumulator;

    [ViewVariables(VVAccess.ReadOnly)]
    public float UpdateDelay = 6f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsRegenerating;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsLesserForm;

    [ViewVariables(VVAccess.ReadOnly)]
    public string HiveName;

    [ViewVariables(VVAccess.ReadOnly), DataField("absorbedEntities")]
    public Dictionary<string, HumanoidData> AbsorbedEntities = new();

    [ViewVariables]
    public int AbsorbedCount = 0;

    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNACost")]
    public int AbsorbDnaCost;

    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNADelay")]
    public float AbsorbDnaDelay = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("TransformDelay")]
    public float TransformDelay = 2f;

    [ViewVariables(VVAccess.ReadWrite), DataField("RegenerateDelay")]
    public float RegenerateDelay = 60f;

    [ViewVariables(VVAccess.ReadWrite), DataField("LesserFormDelay")]
    public float LesserFormDelay = 5f;

    public bool IsInited;
}

public struct HumanoidData
{
    public EntityPrototype EntityPrototype;

    public MetaDataComponent? MetaDataComponent;

    public HumanoidAppearanceComponent AppearanceComponent;

    public string Name;

    public string Dna;
}
