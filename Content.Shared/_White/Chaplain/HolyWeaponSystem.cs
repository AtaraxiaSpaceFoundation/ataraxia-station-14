using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._White.Chaplain;

public sealed class HolyWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyWeaponComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<HolyWeaponComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup("[color=lightblue]Данное оружие наделено священной силой.[/color]");
    }

    public bool IsHoldingHolyWeapon(EntityUid uid)
    {
        return _hands.EnumerateHeld(uid).Any(HasComp<HolyWeaponComponent>);
    }
}
