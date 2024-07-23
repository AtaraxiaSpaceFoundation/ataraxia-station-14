using Content.Shared._Miracle.Systems;
using Content.Shared._White.Overlays;

namespace Content.Server._Miracle.Systems;

public sealed class ThermalVisionSystem : SharedEnhancedVisionSystem<ThermalVisionComponent,
    TemporaryThermalVisionComponent, ToggleThermalVisionEvent>
{
}
