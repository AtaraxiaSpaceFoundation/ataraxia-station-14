using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Server.Actions;
using Content.Shared.Animations;
using static Content.Shared.Animations.EmoteAnimationComponent;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;

namespace Content.Server.Animations;

public class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] public readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// We write 'EmoteAction' word before id name for instant action.
    /// Example: EmoteActionJump, EmoteActionFlip and etc.
    /// </summary>
    private const string INSTANT_IDENTIFIER = "EmoteAction";
    public override void Initialize()
    {
        SubscribeLocalEvent<EmoteAnimationComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EmoteAnimationComponent, MapInitEvent>(OnMapInint);
        SubscribeLocalEvent<EmoteAnimationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmoteAnimationComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<EmoteAnimationComponent, EmoteActionEvent>(OnEmoteAction);
    }

    private void OnGetState(EntityUid uid, EmoteAnimationComponent component, ref ComponentGetState args)
    {
        args.State = new EmoteAnimationComponentState(component.AnimationId);
    }

    private void OnMapInint(EntityUid uid, EmoteAnimationComponent component, MapInitEvent args)
    {
        foreach (var item in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (item.ID.Length <= INSTANT_IDENTIFIER.Length ||
                item.ID[..INSTANT_IDENTIFIER.Length] != INSTANT_IDENTIFIER)
                continue;

            EntityUid? action = null;
            component.Actions.Add(action);
            _action.AddAction(uid, ref action, item.ID);
        }
    }

    private void OnShutdown(EntityUid uid, EmoteAnimationComponent component, ComponentShutdown args)
    {
        foreach (var item in component.Actions)
        {
            _action.RemoveAction(uid, item);
        }
    }

    private void OnEmote(EntityUid uid, EmoteAnimationComponent component, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Gesture))
            return;

        PlayEmoteAnimation(uid, component, args.Emote.ID);
    }

    private void OnEmoteAction(EntityUid uid, EmoteAnimationComponent component, EmoteActionEvent args)
    {
        PlayEmoteAnimation(uid, component, args.Emote);
    }

    public void PlayEmoteAnimation(EntityUid uid, EmoteAnimationComponent component, string emoteId)
    {
        component.AnimationId = emoteId;
        Dirty(component);
    }
}
