using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedIdCardConsoleSystem))]
public sealed partial class IdCardConsoleComponent : Component
{
    public const int MaxFullNameLength = 30;
    public const int MaxJobTitleLength = 30;

    public static string PrivilegedIdCardSlotId = "IdCardConsole-privilegedId";
    public static string TargetIdCardSlotId = "IdCardConsole-targetId";

    [DataField]
    public ItemSlot PrivilegedIdSlot = new();

    [DataField]
    public ItemSlot TargetIdSlot = new();

    [Serializable, NetSerializable]
    public sealed class WriteToTargetIdMessage : BoundUserInterfaceMessage
    {
        public readonly string FullName;
        public readonly string JobTitle;
        public readonly List<ProtoId<AccessLevelPrototype>> AccessList;
        public readonly ProtoId<AccessLevelPrototype> JobPrototype;
        public readonly string? SelectedIcon; //WD-EDIT

        public WriteToTargetIdMessage(string fullName, string jobTitle, List<ProtoId<AccessLevelPrototype>> accessList, ProtoId<AccessLevelPrototype> jobPrototype,
            string? selectedIcon)
        {
            FullName = fullName;
            JobTitle = jobTitle;
            AccessList = accessList;
            JobPrototype = jobPrototype;
            SelectedIcon = selectedIcon;
        }
    }

    // Put this on shared so we just send the state once in PVS range rather than every time the UI updates.

    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> AccessLevels = new()
    {
        "Armory",
        "Atmospherics",
        "Bar",
        "Brig",
        "Detective",
        "Captain",
        "Cargo",
        "Chapel",
        "Chemistry",
        "ChiefEngineer",
        "ChiefMedicalOfficer",
        "Command",
        "Cryogenics",
        "Engineering",
        "External",
        "HeadOfPersonnel",
        "HeadOfSecurity",
        "Hydroponics",
        "Janitor",
        "Kitchen",
        "Lawyer",
        "Maintenance",
        "Medical",
        "Quartermaster",
        "Research",
        "ResearchDirector",
        "Salvage",
        "Security",
        "Service",
        "Theatre"
    };

    // WD edit
    [DataField, AutoNetworkedField]
    public List<List<ProtoId<AccessLevelPrototype>>> AccessLevelsConsole = new()
    {
        new List<ProtoId<AccessLevelPrototype>> {"Captain", "HeadOfPersonnel", "HeadOfSecurity", "ChiefMedicalOfficer", "ChiefEngineer", "ResearchDirector", "Command"}, // Command
        new List<ProtoId<AccessLevelPrototype>> {"Armory", "Brig", "Security","Detective", "Lawyer"}, // Security
        new List<ProtoId<AccessLevelPrototype>> {"Chemistry", "Cryogenics", "Medical"}, // Medical
        new List<ProtoId<AccessLevelPrototype>> {"Atmospherics", "Engineering", "External", "Maintenance"}, // Engineering
        new List<ProtoId<AccessLevelPrototype>> {"Research"}, // Researching
        new List<ProtoId<AccessLevelPrototype>> {"Cargo", "Salvage"}, // Cargo
        new List<ProtoId<AccessLevelPrototype>> { "Service", "Theatre", "Bar", "Chapel", "Hydroponics", "Janitor", "Kitchen"} // Service
    };

    //WD-EDIT
    // Command, Service, Security, Medical, Engineering, Researching, Cargo,
    [DataField("jobIcons")]
    public List<List<string>> JobIcons = new()
    {
        new List<string> {"Captain", "HeadOfPersonnel", "HeadOfSecurity", "ChiefMedicalOfficer", "ChiefEngineer", "ResearchDirector", "QuarterMaster", "Inspector"},
        new List<string> {"HeadOfPersonnel", "Lawyer", "Clown", "Bartender", "Reporter", "Chef", "Botanist", "ServiceWorker", "Zookeeper", "Musician", "Librarian", "Janitor", "Chaplain", "Mime",  "Boxer", "Passenger", "Visitor", "Borg", "CustomId"},
        new List<string> {"HeadOfSecurity", "Warden",  "SeniorOfficer", "SecurityOfficer", "Detective", "SecurityCadet", "Brigmedic", "Lawyer"},
        new List<string> {"ChiefMedicalOfficer", "SeniorPhysician", "Paramedic", "Chemist", "MedicalDoctor", "Virologist", "Geneticist", "MedicalIntern", "Psychologist"},
        new List<string> {"ChiefEngineer", "SeniorEngineer", "AtmosphericTechnician", "StationEngineer", "TechnicalAssistant"},
        new List<string> {"ResearchDirector", "SeniorResearcher",  "Scientist", "Roboticist", "ResearchAssistant"},
        new List<string> {"QuarterMaster", "ShaftMiner", "CargoTechnician"},
    };
    // WD EDIT END


    [Serializable, NetSerializable]
    public sealed class IdCardConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string PrivilegedIdName;
        public readonly bool IsPrivilegedIdPresent;
        public readonly bool IsPrivilegedIdAuthorized;
        public readonly bool IsTargetIdPresent;
        public readonly string TargetIdName;
        public readonly string? TargetIdFullName;
        public readonly string? TargetIdJobTitle;
        public readonly List<ProtoId<AccessLevelPrototype>>? TargetIdAccessList;
        public readonly List<ProtoId<AccessLevelPrototype>>? AllowedModifyAccessList;
        public readonly ProtoId<AccessLevelPrototype> TargetIdJobPrototype;
        public readonly string? TargetIdJobIcon; //WD-EDIT

        public IdCardConsoleBoundUserInterfaceState(
            bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            bool isTargetIdPresent,
            string? targetIdFullName,
            string? targetIdJobTitle,
            List<ProtoId<AccessLevelPrototype>>? targetIdAccessList,
            List<ProtoId<AccessLevelPrototype>>? allowedModifyAccessList,
            ProtoId<AccessLevelPrototype> targetIdJobPrototype,
            string privilegedIdName,
            string targetIdName,
            string? targetIdJobIcon) // #WD EDIT (targetIdJobIcon)
        {
            IsPrivilegedIdPresent = isPrivilegedIdPresent;
            IsPrivilegedIdAuthorized = isPrivilegedIdAuthorized;
            IsTargetIdPresent = isTargetIdPresent;
            TargetIdFullName = targetIdFullName;
            TargetIdJobTitle = targetIdJobTitle;
            TargetIdAccessList = targetIdAccessList;
            AllowedModifyAccessList = allowedModifyAccessList;
            TargetIdJobPrototype = targetIdJobPrototype;
            PrivilegedIdName = privilegedIdName;
            TargetIdName = targetIdName;
            TargetIdJobIcon = targetIdJobIcon; //WD-EDIT
        }
    }

    [Serializable, NetSerializable]
    public enum IdCardConsoleUiKey : byte
    {
        Key
    }
}
