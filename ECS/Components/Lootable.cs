using System.Collections.Generic;

namespace EldmeresTale.ECS.Components;

public struct Lootable {

	public Dictionary<string, float> LootTable;

	public int CoinMin;
	public int CoinMax;
	public float CoinDropChance;

	public int HealthAmount;
	public float HealthDropChance;

	public int XPAmount;

	public Lootable() {
		LootTable = [];
	}

	public Lootable(string lootId, float chance) : this(new Dictionary<string, float> { { lootId, chance } }) { }

	public Lootable(Dictionary<string, float> table) : this() {
		foreach (KeyValuePair<string, float> item in table) {
			LootTable.Add(item.Key, item.Value);
		}
	}

	public Lootable(Dictionary<string, float> table, int xpValue, int coinValue) : this(table) {
		CoinMin = 0;
		CoinMax = coinValue;
		CoinDropChance = 1;
		XPAmount = xpValue;
	}
}
