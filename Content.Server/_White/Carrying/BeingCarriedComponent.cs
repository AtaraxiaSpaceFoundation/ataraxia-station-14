namespace Content.Server._White.Carrying;

/// <summary>
/// Stores the carrier of an entity being carried.
/// </summary>
[RegisterComponent]
public sealed partial class BeingCarriedComponent : Component
{
    public EntityUid Carrier = default!;
}