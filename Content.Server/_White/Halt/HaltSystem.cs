using System.Linq;
using Content.Shared.Actions;
using Content.Server.Chat.Systems;
using Robust.Shared.Utility;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared._White.Other;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.Halt
{
    public sealed class HaltSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HaltComponent, GetItemActionsEvent>(OnGetEquipped);
            SubscribeLocalEvent<HaltComponent, HaltAction>(Act);
        }

        private void OnGetEquipped(EntityUid uid, HaltComponent component, GetItemActionsEvent args)
        {
            if (args.SlotFlags != SlotFlags.MASK)
                return;

            if (!HasComp<HumanoidAppearanceComponent>(args.User))
                return;

            args.AddAction(ref component.ActionEntity, "Halt");
        }

        private void Act(EntityUid uid, HaltComponent component, HaltAction args)
        {
            if (args.Handled)
                return;

            if (!component.PhraseToSoundMap.Any())
                return;

            var randomIndex = _random.Next(component.PhraseToSoundMap.Count);
            var selectedPhrase = component.PhraseToSoundMap.Keys.ElementAt(randomIndex);
            var selectedSound = component.PhraseToSoundMap[selectedPhrase];

            _audio.PlayPvs(selectedSound, uid);

            var hMessage = Loc.GetString(selectedPhrase);
            var wrappedMessage = Loc.GetString(
                component.ChatLoc,
                ("entityName", args.Performer),
                ("hMessage", FormattedMessage.EscapeText(hMessage)),
                ("color", component.ChatColor)
            );

            _chat.SendInVoiceRange(ChatChannel.Local, hMessage, wrappedMessage, args.Performer, ChatTransmitRange.Normal);

            args.Handled = true;
        }
    }
}
