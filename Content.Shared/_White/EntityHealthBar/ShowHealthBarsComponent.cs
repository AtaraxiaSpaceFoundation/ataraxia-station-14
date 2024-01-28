using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._White.EntityHealthBar
{
    /// <summary>
    /// This component allows you to see health bars above damageable mobs.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ShowWhiteHealthBarsComponent : Component
    {
        /// <summary>
        /// Displays health bars of the damage containers.
        /// </summary>

        [DataField("damageContainers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
        public List<string>? DamageContainers;
    }
}
