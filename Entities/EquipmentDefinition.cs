using System;
using System.Text.Json.Serialization;

namespace EldmeresTale.Entities;

public class EquipmentDefinition {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("description")]
	public string Description { get; set; }

	[JsonPropertyName("slot")]
	public string SlotString { get; set; }

	[JsonIgnore]
	public EquipmentSlot Slot {
		get => Enum.Parse<EquipmentSlot>(SlotString);
		set => SlotString = value.ToString();
	}

	[JsonPropertyName("rarity")]
	public string RarityString { get; set; }

	[JsonIgnore]
	public EquipmentRarity Rarity {
		get => Enum.Parse<EquipmentRarity>(RarityString);
		set => RarityString = value.ToString();
	}

	[JsonPropertyName("requiredLevel")]
	public int RequiredLevel { get; set; } = 1;

	// Combat stats
	[JsonPropertyName("attackDamage")]
	public int AttackDamage { get; set; } = 0;

	[JsonPropertyName("defense")]
	public int Defense { get; set; } = 0;

	[JsonPropertyName("maxHealth")]
	public int MaxHealth { get; set; } = 0;

	// Advanced combat
	[JsonPropertyName("attackSpeed")]
	public float AttackSpeed { get; set; } = 0f;

	[JsonPropertyName("critChance")]
	public float CritChance { get; set; } = 0f;

	[JsonPropertyName("critMultiplier")]
	public float CritMultiplier { get; set; } = 0f;

	[JsonPropertyName("lifeSteal")]
	public float LifeSteal { get; set; } = 0f;

	[JsonPropertyName("dodgeChance")]
	public float DodgeChance { get; set; } = 0f;

	// Regeneration
	[JsonPropertyName("healthRegen")]
	public float HealthRegen { get; set; } = 0f;

	// Movement
	[JsonPropertyName("speed")]
	public float Speed { get; set; } = 0f;

	// Visual (for future sprite loading)
	[JsonPropertyName("spriteKey")]
	public string SpriteKey { get; set; }
}
