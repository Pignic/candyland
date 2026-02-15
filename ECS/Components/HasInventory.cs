using EldmeresTale.Core;

namespace EldmeresTale.ECS.Components;

public struct HasInventory {
	public Inventory Inventory;

	public HasInventory() {
		Inventory = new Inventory();
	}
}
