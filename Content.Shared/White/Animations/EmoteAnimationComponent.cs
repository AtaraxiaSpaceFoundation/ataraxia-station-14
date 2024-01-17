using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Animations;

/// <summary>
/// Event for playing animations
/// </summary>
public sealed partial class EmoteActionEvent : InstantActionEvent
{
    [ViewVariables]
    [DataField("emote", readOnly: true, required: true)]
    public string Emote = default!;
};

[RegisterComponent]
[NetworkedComponent]
public sealed partial class EmoteAnimationComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string AnimationId = "none";

    public readonly List<EntityUid?> Actions = new();

    [Serializable, NetSerializable]
    public class EmoteAnimationComponentState : ComponentState
    {
        public string AnimationId { get; init; }

        public EmoteAnimationComponentState(string animationId)
        {
            AnimationId = animationId;
        }
    }
}
