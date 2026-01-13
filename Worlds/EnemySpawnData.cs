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
	public float? XpMultiplier { get; set; }

	[JsonPropertyName("lootTable")]
	public string[] LootTable { get; set; }  // Override loot table

	[JsonPropertyName("lootChance")]
	public float? LootChance { get; set; }  // Override loot chance

	[JsonPropertyName("coinMin")]
	public int? CoinMin { get; set; }

	[JsonPropertyName("coinMax")]
	public int? CoinMax { get; set; }
}