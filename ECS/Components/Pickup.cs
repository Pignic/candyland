
namespace EldmeresTale.ECS.Components;

public enum PickupType {
	Health,
	Coin,
	BigCoin,
	XP,
	Material
}

public struct Pickup {
	public PickupType Type;
	public string Name;
	public int Value;

	public Pickup(PickupType type, int value, string name = "") {
		Type = type;
		Value = value;
		Name = name;
	}
}