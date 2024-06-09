using Content.Server.RoundEnd;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Wizard;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class WizardRuleComponent : Component
{
    public readonly List<EntityUid> WizardMinds = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? TargetStation;

    [DataField("minPlayers")]
    public int MinPlayers = 20;

    [DataField("announcementOnWizardDeath")]
    public bool AnnouncementOnWizardDeath = true;

    [DataField("points")]
    public int Points = 10; //TODO: wizard shop prototype

    [DataField("wizardRoleProto")]
    public ProtoId<AntagPrototype> WizardRoleProto = "WizardRole";

    [DataField("wizardSpawnPointProto")]
    public EntProtoId SpawnPointProto = "SpawnPointWizard";

    [DataField]
    public EntProtoId GhostSpawnPointProto = "SpawnPointGhostWizard";

    [DataField("startingGear")]
    public ProtoId<StartingGearPrototype> StartingGear = "WizardGear";

    [DataField("spawnShuttle")]
    public bool SpawnShuttle = true;

    [DataField]
    public EntityUid? ShuttleMap;

    [DataField("shuttlePath")]
    public string ShuttlePath = "/Maps/White/Shuttles/wizard.yml";

    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "Wizard";

    public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.Nothing;

    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-centcom";

    [DataField]
    public string RoundEndTextShuttleCall = "wizard-no-more-threat-announcement-shuttle-call";

    [DataField]
    public string RoundEndTextAnnouncement = "wizard-no-more-threat-announcement";

    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(5);

    [DataField]
    public ProtoId<WeightedRandomPrototype> ObjectiveGroup = "WizardObjectiveGroups";
}
