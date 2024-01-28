using System.Numerics;
using Content.Shared._White.Spline;
using Content.Shared._White.Trail;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Server._White.Trail;

[RegisterComponent]
public sealed partial class TrailComponent : SharedTrailComponent
{
    public TrailComponent()
    {
        var defaultTrail = TrailSettings.Default;
        Scale = defaultTrail.Scale;
        СreationDistanceThresholdSquared = defaultTrail.СreationDistanceThresholdSquared;
        СreationMethod = defaultTrail.СreationMethod;
        CreationOffset = defaultTrail.CreationOffset;
        Gravity = defaultTrail.Gravity;
        MaxRandomWalk = defaultTrail.MaxRandomWalk;
        Lifetime = defaultTrail.Lifetime;
        TexurePath = defaultTrail.TexurePath;
        Gradient = defaultTrail.Gradient;
        GradientIteratorType = defaultTrail.GradientIteratorType;
    }

    public override Vector2 Gravity { get; set; }

    public override float Lifetime { get; set; }

    public override Vector2 MaxRandomWalk { get; set; }

    public override Vector2 Scale { get; set; }

    public override string? TexurePath { get; set; }

    public override Vector2 CreationOffset { get; set; }

    public override float СreationDistanceThresholdSquared { get; set; }

    public override SegmentCreationMethod СreationMethod { get; set; }

    public override Vector4[] Gradient { get; set; }

    public override float LengthStep { get; set; }

    public override Spline2DType SplineIteratorType { get; set; }

    public override TrailSplineRendererType SplineRendererType { get; set; }

    public override Spline4DType GradientIteratorType { get; set; }
}
