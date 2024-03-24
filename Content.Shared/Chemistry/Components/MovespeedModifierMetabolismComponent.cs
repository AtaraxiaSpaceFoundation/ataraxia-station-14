using System.Threading;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class MovespeedModifierMetabolismComponent : Component
    {
        [ViewVariables, DataField]
        public CancellationTokenSource CancelTokenSource = new();

        [AutoNetworkedField, ViewVariables]
        public float WalkSpeedModifier { get; set; }

        [AutoNetworkedField, ViewVariables]
        public float SprintSpeedModifier { get; set; }

        /// <summary>
        /// When the current modifier is expected to end.
        /// </summary>
        [AutoNetworkedField, ViewVariables] // WD EDIT
        public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;
    }
}

