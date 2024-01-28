using Content.Shared.White.Trail;
using Robust.Shared.Map;

namespace Content.Client._White.Trail.Line.Manager;

public interface ITrailLineManager
{
    IEnumerable<ITrailLine> Lines { get; }

    ITrailLine CreateTrail(ITrailSettings settings, MapId mapId);

    void Detach(ITrailLineHolder holder);

    void Update(float dt);
}
