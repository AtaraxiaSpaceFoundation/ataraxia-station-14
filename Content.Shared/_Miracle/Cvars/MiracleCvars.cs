using Robust.Shared.Configuration;

namespace Content.Shared._Miracle.Cvars;

[CVarDefs]
public sealed class MiracleCvars
{
    // <points> / <ratio> = <time_in_seconds>
    // 100 / 10 = 10
    public static readonly CVarDef<double> GulagPointsToTimeRatio = CVarDef.Create("miracle.gulag.points_to_time",
        0.01d, CVar.SERVERONLY, "<points> / <ratio> = <time_in_seconds>");
}
