using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._White.Other.MeleeBlockSystem;

public sealed class MeleeBlockSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, MeleeBlockAttemptEvent>(OnBlockAttempt);
    }

    private void OnBlockAttempt(Entity<HandsComponent> ent, ref MeleeBlockAttemptEvent args)
    {
        if (ent.Owner == args.Attacker ||
            !TryComp(ent.Comp.ActiveHandEntity, out MeleeBlockComponent? blockComponent) ||
            !_random.Prob(blockComponent.BlockChance))
            return;

        args.Blocked = true;

        _popupSystem.PopupEntity("заблокировал!", ent);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg"), ent,
            AudioParams.Default.WithVariation(0.25f));
    }
}
