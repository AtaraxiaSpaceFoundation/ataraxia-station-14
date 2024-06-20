using System.Linq;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Content.Shared.Access;
using Content.Shared.Access.Systems;

namespace Content.Client.Access.UI;

[GenerateTypedNameReferences]
public sealed partial class AccessLevelControl : GridContainer
{
    public readonly Dictionary<ProtoId<AccessLevelPrototype>, Button> ButtonsList = new();
    public readonly List<Dictionary<ProtoId<AccessLevelPrototype>, Button>> ButtonGroups = new ();

    public AccessLevelControl()
    {
        RobustXamlLoader.Load(this);
    }

    public void Populate(List<ProtoId<AccessLevelPrototype>> accessLevels, IPrototypeManager prototypeManager)
    {
        foreach (var access in accessLevels)
        {
            if (!prototypeManager.TryIndex(access, out var accessLevel))
            {
                Logger.Error($"Unable to find accesslevel for {access}");
                continue;
            }

            var newButton = new Button
            {
                Text = accessLevel.GetAccessLevelName(),
                ToggleMode = true,
            };
            AddChild(newButton);
            ButtonsList.Add(accessLevel.ID, newButton);
        }
    }

    public void PopulateForConsole(List<List<ProtoId<AccessLevelPrototype>>> accessLevels, IPrototypeManager prototypeManager)
    {
        var departmentColors = new List<String> // Colors from StyleNano.cs
        {
            "ButtonColorCommandDepartment",
            "ButtonColorSecurityDepartment",
            "ButtonColorMedicalDepartment",
            "ButtonColorEngineeringDepartment",
            "ButtonColorResearchingDepartment",
            "ButtonColorCargoDepartment",
            "ButtonColorServiceDepartment"
        };
        var currentColorIndex = 0;

        foreach (var department in accessLevels)
        {
            Dictionary<ProtoId<AccessLevelPrototype>, Button> buttons = new();
            foreach (var access in department)
            {
                if (!prototypeManager.TryIndex(access, out var accessLevel))
                {
                    Logger.Error($"Unable to find accesslevel for {access}");
                    continue;
                }

                var newButton = new Button
                {
                    Text = accessLevel.GetAccessLevelName(),
                    ToggleMode = true,
                };

                newButton.AddStyleClass(departmentColors[currentColorIndex]);
                buttons.Add(accessLevel.ID, newButton);
            }

            ButtonGroups.Add(buttons);
            currentColorIndex++;
        }
    }

    public void UpdateStateConsole(
        List<ProtoId<AccessLevelPrototype>> pressedList,
        List<ProtoId<AccessLevelPrototype>>? enabledList = null)
    {
        foreach (var department in ButtonGroups)
        {
            foreach (var (accessName, button) in department)
            {
                button.Pressed = pressedList.Contains(accessName);
                button.Disabled = !(enabledList?.Contains(accessName) ?? true);
            }
        }

    }

    public void UpdateState(
        List<ProtoId<AccessLevelPrototype>> pressedList,
        List<ProtoId<AccessLevelPrototype>>? enabledList = null)
    {
        foreach (var (accessName, button) in ButtonsList)
        {
            button.Pressed = pressedList.Contains(accessName);
            button.Disabled = !(enabledList?.Contains(accessName) ?? true);
        }
    }
}
