using Robust.Shared.Serialization;

namespace Content.Shared._White.Chaplain;

[Serializable, NetSerializable]
public enum SelectWeaponUi
{
    Key
}

[Serializable, NetSerializable]
public class WeaponSelectedEvent : BoundUserInterfaceMessage
{
    public string SelectedWeapon;

    public WeaponSelectedEvent(string weapon)
    {
        SelectedWeapon = weapon;
    }
}
