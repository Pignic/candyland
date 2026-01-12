using System.Collections.Generic;

namespace EldmeresTale.ECS.Components;

public struct Lootable {
	public Dictionary<string, float> LootTable;

	public Lootable() {
		LootTable = [];
	}

	public Lootable(string lootId, float chance) : this(new Dictionary<string, float> { { lootId, chance } }) { }

	public Lootable(Dictionary<string, float> table) : this() {
		foreach (KeyValuePair<string, float> item in table) {
			LootTable.Add(item.Key, item.Value);
		}
	}
}
