using System.Threading;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Cult.Components;

/// <summary>
/// This is used for tagging a mob as a cultist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultistComponent : Component
{
    [DataField("greetSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? CultistGreetSound = new SoundPathSpecifier("/Audio/CultSounds/fart.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("holyConvertTime")]
    public float HolyConvertTime = 15f;

    public CancellationTokenSource? HolyConvertToken;

    [NonSerialized]
    public List<string> SelectedEmpowers = new();

    public static string SummonCultDaggerAction = "InstantActionSummonCultDagger";

    public static string BloodRitesAction = "InstantActionBloodRites";

    public static string CultTwistedConstructionAction = "ActionCultTwistedConstruction";

    public static string CultTeleportAction = "ActionCultTeleport";

    public static string CultSummonCombatEquipmentAction = "ActionCultSummonCombatEquipment";

    public static List<string> CultistActions = new()
    {
        SummonCultDaggerAction, BloodRitesAction, CultTwistedConstructionAction, CultTeleportAction,
        CultSummonCombatEquipmentAction
    };
}
