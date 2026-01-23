using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EldmeresTale.Worlds;

public class EnemySpawnData {

	[JsonPropertyName("enemyId")]
	public string EnemyId { get; set; }

	[JsonPropertyName("x")]
	public float X { get; set; }

	[JsonPropertyName("y")]
	public float Y { get; set; }

	[JsonPropertyName("behavior")]
	public string Behavior { get; set; }  // Optional override

	[JsonPropertyName("patrolStartX")]
	public float? PatrolStartX { get; set; }

	[JsonPropertyName("patrolStartY")]
	public float? PatrolStartY { get; set; }

	[JsonPropertyName("patrolEndX")]
	public float? PatrolEndX { get; set; }

	[JsonPropertyName("patrolEndY")]
	public float? PatrolEndY { get; set; }

	[JsonPropertyName("healthMultiplier")]
	public float? HealthMultiplier { get; set; }

	[JsonPropertyName("damageMultiplier")]
	public float? DamageMultiplier { get; set; }

	[JsonPropertyName("speedMultiplier")]
	public float? SpeedMultiplier { get; set; }

	[JsonPropertyName("xpMultiplier")]
	public float? XpMultiplier { get; set; } = 1;

	[JsonPropertyName("coinMultiplier")]
	public float? CoinMultiplier { get; set; } = 1;

	[JsonPropertyName("lootTable")]
	public JsonElement[][] LootTableRaw { get; set; }

	[JsonIgnore]
	private Dictionary<string, float> LootTable;

	public Dictionary<string, float> GetLootTable(bool reload = false) {
		if (LootTable == null || reload) {
			LootTable = [];
			foreach (JsonElement[] value in LootTableRaw) {
				LootTable.TryAdd(value[0].GetString(), value[1].GetSingle());
			}
		}
		return LootTable;
	}

	public bool HasLootTable() {
		return LootTableRaw?.Length > 0;
	}
}