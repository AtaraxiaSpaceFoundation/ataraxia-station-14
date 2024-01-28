using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Shared._White.MeatyOre;

[Serializable, NetSerializable]
public sealed class MeatyOreShopRequestEvent : EntityEventArgs
{
}

[NetworkedComponent, RegisterComponent]
public sealed partial class IgnorBUIInteractionRangeComponent : Component
{

}
