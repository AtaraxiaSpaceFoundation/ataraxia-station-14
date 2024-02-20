using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._White.Chaplain;
using Content.Shared.Ghost;

namespace Content.Server._White.Chaplain;

public sealed class NullRodSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NullRodComponent, WeaponSelectedEvent>(OnWeaponSelected);
    }

    private void OnWeaponSelected(Entity<NullRodComponent> ent, ref WeaponSelectedEvent args)
    {
        var entity = args.Session.AttachedEntity;

        if (args.SelectedWeapon == string.Empty || entity == null)
            return;

        if (!HasComp<HolyComponent>(entity.Value) && !HasComp<GhostComponent>(entity.Value))
        {
            _popup.PopupEntity($"Вам не хватает веры, чтобы использовать {Name(ent)}", entity.Value, entity.Value);
            return;
        }

        var weapon = Spawn(args.SelectedWeapon, Transform(entity.Value).Coordinates);
        EnsureComp<HolyWeaponComponent>(weapon);

        Del(ent);

        _hands.PickupOrDrop(entity.Value, weapon, true, false, false);
    }
}
