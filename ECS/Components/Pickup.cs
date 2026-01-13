
namespace EldmeresTale.ECS.Components;

public enum PickupType {
	Health,
	Coin,
	BigCoin,
	XP
}

public struct Pickup {
	public PickupType Type;
	public int Value;

	public Pickup(PickupType type, int value) {
		Type = type;
		Value = value;
	}
}