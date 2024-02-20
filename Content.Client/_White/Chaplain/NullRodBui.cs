using Content.Client._White.UserInterface.Radial;
using Content.Shared._White.Chaplain;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Chaplain;

[UsedImplicitly]
public sealed class NullRodBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private SpriteSystem _spriteSystem = default!;

    private bool _selected;
    private RadialContainer? _weaponSelector;

    public NullRodBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _spriteSystem = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        var nullRod = _entityManager.GetComponent<NullRodComponent>(Owner);

        _weaponSelector = new RadialContainer();

        _weaponSelector.Closed += () =>
        {
            if (_selected)
                return;

            SendMessage(new WeaponSelectedEvent(string.Empty));
            Close();
        };

        foreach (var weapon in nullRod.Weapons)
        {
            var weaponPrototype = _prototypeManager.Index<EntityPrototype>(weapon);
            var button = _weaponSelector.AddButton(weaponPrototype.Name,
                _spriteSystem.GetPrototypeIcon(weaponPrototype).Default);

            button.Controller.OnPressed += _ =>
            {
                _selected = true;
                SendMessage(new WeaponSelectedEvent(weapon));
                _weaponSelector.Close();
                Close();
            };
        }

        _weaponSelector.OpenAttachedLocalPlayer();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _weaponSelector?.Close();
    }
}
