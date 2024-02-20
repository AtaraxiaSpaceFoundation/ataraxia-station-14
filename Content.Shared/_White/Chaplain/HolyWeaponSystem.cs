using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._White.Chaplain;

public sealed class HolyWeaponSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyWeaponComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HolyWeaponComponent, AttemptMeleeEvent>(OnMeleeAttempt);
    }

    private void OnMeleeAttempt(Entity<HolyWeaponComponent> ent, ref AttemptMeleeEvent args)
    {
        if (HasComp<HolyComponent>(args.User) || HasComp<GhostComponent>(args.User))
            return;

        args.Cancelled = true;
        args.Message = $"Вам не хватает веры, чтобы использовать {Name(ent)}";
    }

    private void OnExamined(Entity<HolyWeaponComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup("[color=lightblue]Данное оружие наделено священной силой.[/color]");
    }
}
