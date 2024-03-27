using System.Threading;

namespace Content.Server._White.Carrying;

[RegisterComponent]
public sealed partial class CarriableComponent : Component
{
    /// <summary>
    ///     Number of free hands required
    ///     to carry the entity
    /// </summary>
    [DataField]
    public int FreeHandsRequired = 2;

    public CancellationTokenSource? CancelToken;
}