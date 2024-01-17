using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared.Animations;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using static Content.Shared.Animations.EmoteAnimationComponent;

namespace Content.Server.White.Animations;

public sealed class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// We write 'EmoteAction' word before id name for instant action.
    /// Example: EmoteActionJump, EmoteActionFlip and etc.
    /// </summary>
    private const string InstantIdentifier = "EmoteAction";

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
            if (item.ID.Length <= InstantIdentifier.Length ||
                item.ID[..InstantIdentifier.Length] != InstantIdentifier)
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
        args.Handled = true;
    }

    public void PlayEmoteAnimation(EntityUid uid, EmoteAnimationComponent component, string emoteId)
    {
        component.AnimationId = emoteId;
        Dirty(component);
    }
}
