using Content.Client._White.UserInterface.Radial;
using Content.Client.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Popups;
using Content.Shared._White.Cult.Structures;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Cult.UI.StructureRadial;

public sealed class StructureCraftBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private RadialContainer? _radialContainer;

    public StructureCraftBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    private void CreateUI()
    {
        if (_radialContainer != null)
            ResetUI();

        _radialContainer = new RadialContainer();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CultStructurePrototype>())
        {
            var radialButton = _radialContainer.AddButton(prototype.StructureName, prototype.Icon);
            radialButton.Controller.OnPressed += _ =>
            {
                Select(prototype.StructureId);
            };
        }

        _radialContainer.OpenAttachedLocalPlayer();
    }

    private void ResetUI()
    {
        _radialContainer?.Close();
        _radialContainer = null;
    }

    protected override void Open()
    {
        base.Open();

        CreateUI();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        ResetUI();
    }

    private void Select(string id)
    {
        CreateBlueprint(id);
        ResetUI();
        Close();
    }

    private void CreateBlueprint(string id)
    {
        var newObj = new PlacementInformation
        {
            Range = 2,
            IsTile = false,
            EntityType = id,
            PlacementOption = "SnapgridCenter"
        };

        _prototypeManager.TryIndex<ConstructionPrototype>(id, out var construct);

        if (construct == null)
            return;

        var player = _player.LocalEntity;

        if (player == null)
            return;

        PlacementHijack hijack;

        if (construct.ID == "CultPylon")
        {
            hijack = new CultPylonPlacementHijack(construct, _entMan, player.Value);
        }
        else
        {
            var constructSystem = _systemManager.GetEntitySystem<ConstructionSystem>();
            hijack = new ConstructionPlacementHijack(constructSystem, construct);
        }

        _placement.BeginPlacing(newObj, hijack);
    }
}
