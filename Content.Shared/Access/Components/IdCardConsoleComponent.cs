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
        public readonly List<string> AccessList;
        public readonly string JobPrototype;
        public readonly string? SelectedIcon; //WD-EDIT

        public WriteToTargetIdMessage(
            string fullName,
            string jobTitle,
            List<string> accessList,
            string jobPrototype,
            string? selectedIcon) //WD-EDIT (selectedIcon)
        {
            FullName = fullName;
            JobTitle = jobTitle;
            AccessList = accessList;
            JobPrototype = jobPrototype;
            SelectedIcon = "JobIcon" + selectedIcon;
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

    //WD-EDIT
    [DataField("jobIcons")]
    public List<string> JobIcons = new()
    {
        "AtmosphericTechnician",
        "Bartender",
        "Botanist",
        "Boxer",
        "Brigmedic",
        "Captain",
        "CargoTechnician",
        "Chaplain",
        "Chef",
        "Chemist",
        "ChiefEngineer",
        "ChiefMedicalOfficer",
        "Clown",
        "Detective",
        "Geneticist",
        "HeadOfPersonnel",
        "HeadOfSecurity",
        "Janitor",
        "Lawyer",
        "Librarian",
        "MedicalDoctor",
        "MedicalIntern",
        "Mime",
        "Musician",
        "Paramedic",
        "Passenger",
        "Psychologist",
        "QuarterMaster",
        "Reporter",
        "ResearchAssistant",
        "ResearchDirector",
        "Roboticist",
        "Scientist",
        "SecurityCadet",
        "SecurityOfficer",
        "SeniorEngineer",
        "SeniorOfficer",
        "SeniorResearcher",
        "ServiceWorker",
        "ShaftMiner",
        "StationEngineer",
        "TechnicalAssistant",
        "Virologist",
        "Warden",
        "Zookeeper"
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
        public readonly string[]? TargetIdAccessList;
        public readonly string[]? AllowedModifyAccessList;
        public readonly string TargetIdJobPrototype;
        public readonly string? TargetIdJobIcon; //WD-EDIT

        public IdCardConsoleBoundUserInterfaceState(
            bool isPrivilegedIdPresent,
            bool isPrivilegedIdAuthorized,
            bool isTargetIdPresent,
            string? targetIdFullName,
            string? targetIdJobTitle,
            string[]? targetIdAccessList,
            string[]? allowedModifyAccessList,
            string targetIdJobPrototype,
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
