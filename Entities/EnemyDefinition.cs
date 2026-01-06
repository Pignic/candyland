using EldmeresTale.Entities;
using System;
using System.Text.Json.Serialization;

public class EnemyDefinition {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("baseId")]
	public string BaseId { get; set; }  // For variants - inherits from this

	[JsonPropertyName("enemyType")]
	public string EnemyType { get; set; }  // "wolf", "goblin", etc (for quests)

	// === STATS ===
	[JsonPropertyName("health")]
	public int Health { get; set; } = 30;

	[JsonPropertyName("attackDamage")]
	public int AttackDamage { get; set; } = 5;

	[JsonPropertyName("defense")]
	public int Defense { get; set; } = 0;

	[JsonPropertyName("speed")]
	public float Speed { get; set; } = 100f;

	[JsonPropertyName("xpValue")]
	public int XpValue { get; set; } = 25;

	// === BEHAVIOR ===
	[JsonPropertyName("behavior")]
	public string BehaviorString { get; set; } = "Wander";

	[JsonIgnore]
	public EnemyBehavior Behavior {
		get => Enum.Parse<EnemyBehavior>(BehaviorString);
		set => BehaviorString = value.ToString();
	}

	[JsonPropertyName("detectionRange")]
	public float DetectionRange { get; set; } = 150f;

	[JsonPropertyName("wanderInterval")]
	public float WanderInterval { get; set; } = 2f;

	// === DROPS - CURRENCY ===
	[JsonPropertyName("coinMin")]
	public int CoinMin { get; set; } = 0;

	[JsonPropertyName("coinMax")]
	public int CoinMax { get; set; } = 0;

	[JsonPropertyName("coinDropChance")]
	public float CoinDropChance { get; set; } = 0.8f;

	// === DROPS - CONSUMABLES ===
	[JsonPropertyName("healthDropChance")]
	public float HealthDropChance { get; set; } = 0.3f;

	// === DROPS - REAL ITEMS ===
	[JsonPropertyName("lootTable")]
	public string[] LootTable { get; set; }  // Equipment IDs, quest items, etc

	[JsonPropertyName("lootChance")]
	public float LootChance { get; set; } = 0.1f;  // Chance to drop from loot table

	// === VISUAL ===
	[JsonPropertyName("spriteKey")]
	public string SpriteKey { get; set; } = "enemy_idle";

	[JsonPropertyName("isAnimated")]
	public bool IsAnimated { get; set; } = false;

	[JsonPropertyName("frameCount")]
	public int FrameCount { get; set; } = 4;

	[JsonPropertyName("frameWidth")]
	public int FrameWidth { get; set; } = 32;

	[JsonPropertyName("frameHeight")]
	public int FrameHeight { get; set; } = 32;

	[JsonPropertyName("frameTime")]
	public float FrameTime { get; set; } = 0.15f;

	[JsonPropertyName("width")]
	public int Width { get; set; } = 24;

	[JsonPropertyName("height")]
	public int Height { get; set; } = 24;

	// === HELPERS ===

	/// <summary>
	/// Apply properties from base enemy (for variants)
	/// </summary>
	public void InheritFrom(EnemyDefinition baseDef) {
		// Only inherit properties that are still at default values
		if (Name == null) {
			Name = baseDef.Name;
		}

		if (EnemyType == null) {
			EnemyType = baseDef.EnemyType;
		}

		// Stats
		if (Health == 30) {
			Health = baseDef.Health;
		}

		if (AttackDamage == 5) {
			AttackDamage = baseDef.AttackDamage;
		}

		if (Defense == 0) {
			Defense = baseDef.Defense;
		}

		if (Speed == 100f) {
			Speed = baseDef.Speed;
		}

		if (XpValue == 25) {
			XpValue = baseDef.XpValue;
		}

		// Behavior
		if (BehaviorString == "Wander") {
			BehaviorString = baseDef.BehaviorString;
		}

		if (DetectionRange == 150f) {
			DetectionRange = baseDef.DetectionRange;
		}

		if (WanderInterval == 2f) {
			WanderInterval = baseDef.WanderInterval;
		}

		// Drops
		if (CoinMin == 0) {
			CoinMin = baseDef.CoinMin;
		}

		if (CoinMax == 0) {
			CoinMax = baseDef.CoinMax;
		}

		if (CoinDropChance == 0.8f) {
			CoinDropChance = baseDef.CoinDropChance;
		}

		if (HealthDropChance == 0.3f) {
			HealthDropChance = baseDef.HealthDropChance;
		}

		if (LootTable == null) {
			LootTable = baseDef.LootTable;
		}

		if (LootChance == 0.1f) {
			LootChance = baseDef.LootChance;
		}

		// Visual
		if (SpriteKey == "enemy_idle") {
			SpriteKey = baseDef.SpriteKey;
		}

		if (!IsAnimated) {
			IsAnimated = baseDef.IsAnimated;
		}

		if (FrameCount == 4) {
			FrameCount = baseDef.FrameCount;
		}

		if (FrameWidth == 32) {
			FrameWidth = baseDef.FrameWidth;
		}

		if (FrameHeight == 32) {
			FrameHeight = baseDef.FrameHeight;
		}

		if (FrameTime == 0.15f) {
			FrameTime = baseDef.FrameTime;
		}

		if (Width == 24) {
			Width = baseDef.Width;
		}

		if (Height == 24) {
			Height = baseDef.Height;
		}
	}

	public EnemyDefinition Clone() {
		return (EnemyDefinition)MemberwiseClone();
	}
}