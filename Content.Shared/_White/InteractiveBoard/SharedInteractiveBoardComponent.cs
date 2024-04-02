using Robust.Shared.Serialization;
namespace Content.Shared._White.InteractiveBoard;

public abstract partial class SharedInteractiveBoardComponent : Component
{
    [Serializable, NetSerializable]
    public sealed class InteractiveBoardBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly InteractiveBoardAction Mode;

        public InteractiveBoardBoundUserInterfaceState(string text, InteractiveBoardAction mode = InteractiveBoardAction.Read)
        {
            Text = text;
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class InteractiveBoardInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;

        public InteractiveBoardInputTextMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public enum InteractiveBoardUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum InteractiveBoardAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum InteractiveBoardVisuals : byte
    {
        Status
    }

    [Serializable, NetSerializable]
    public enum InteractiveBoardStatus : byte
    {
        Blank,
        Written
    }
}
