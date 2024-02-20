using Content.Client._White.UserInterface.Radial;
using Content.Shared._White.Chaplain;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Chaplain;

[UsedImplicitly]
public sealed class ArmamentsBeaconBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private SpriteSystem _spriteSystem = default!;

    private bool _selected;
    private RadialContainer? _armorSelector;

    public ArmamentsBeaconBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _spriteSystem = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        var beacon = _entityManager.GetComponent<ArmamentsBeaconComponent>(Owner);

        _armorSelector = new RadialContainer();

        _armorSelector.Closed += () =>
        {
            if (_selected)
                return;

            SendMessage(new ArmorSelectedEvent(-1));
            Close();
        };

        for (var i = 0; i < beacon.Armor.Count; i++)
        {
            var armorPrototype = _prototypeManager.Index<EntityPrototype>(beacon.Armor[i]);
            var button = _armorSelector.AddButton(armorPrototype.Name,
                _spriteSystem.GetPrototypeIcon(armorPrototype).Default);

            var index = i;
            button.Controller.OnPressed += _ =>
            {
                _selected = true;
                SendMessage(new ArmorSelectedEvent(index));
                _armorSelector.Close();
                Close();
            };
        }

        _armorSelector.OpenAttachedLocalPlayer();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _armorSelector?.Close();
    }
}
