using Content.Shared.Physics;

namespace Content.Server._White.IncorporealSystem;

[RegisterComponent]
public sealed partial class IncorporealComponent : Component
{
    [DataField] public float MovementSpeedBuff = 1.5f;

    [DataField] public int CollisionMask = (int) CollisionGroup.GhostImpassable;
    [DataField] public int CollisionLayer = 0;
    
    public int StoredMask;
    public int StoredLayer;
}