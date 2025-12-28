using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Entities {
	public enum EquipmentSlot {
		Weapon,      // Right hand
		Helmet,      // Head
		Amulet,      // Neck
		Armor,       // Chest
		Gloves,      // Hands
		Belt,        // Waist
		Pants,       // Legs
		Boots,       // Feet
		Ring         // Finger
	}

	public enum EquipmentRarity {
		Common,     // Gray
		Uncommon,   // Green
		Rare,       // Blue
		Epic,       // Purple
		Legendary   // Orange/Gold
	}

	public class Equipment {
		public string ItemId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public EquipmentSlot Slot { get; set; }
		public EquipmentRarity Rarity { get; set; }
		public Texture2D Icon { get; set; }

		// Stat bonuses this equipment provides
		public int MaxHealthBonus { get; set; }
		public int AttackDamageBonus { get; set; }
		public float SpeedBonus { get; set; }
		public int DefenseBonus { get; set; }
		public float AttackSpeedBonus { get; set; }
		public float CritChanceBonus { get; set; }
		public float CritMultiplierBonus { get; set; }
		public float HealthRegenBonus { get; set; }
		public float LifeStealBonus { get; set; }
		public float DodgeChanceBonus { get; set; }

		// Level requirement
		public int RequiredLevel { get; set; }

		public Equipment(string name, EquipmentSlot slot, EquipmentRarity rarity) {
			Name = name;
			Slot = slot;
			Rarity = rarity;
			RequiredLevel = 1;
		}

		// Get the color for this equipment's rarity
		public Color GetRarityColor() {
			return Rarity switch {
				EquipmentRarity.Common => Color.Gray,
				EquipmentRarity.Uncommon => Color.LimeGreen,
				EquipmentRarity.Rare => Color.DodgerBlue,
				EquipmentRarity.Epic => Color.Purple,
				EquipmentRarity.Legendary => Color.Orange,
				_ => Color.White
			};
		}

		// Apply this equipment's bonuses to player stats
		public void ApplyTo(PlayerStats stats) {
			stats.EquipmentMaxHealth += MaxHealthBonus;
			stats.EquipmentAttackDamage += AttackDamageBonus;
			stats.EquipmentSpeed += SpeedBonus;
			stats.EquipmentDefense += DefenseBonus;
			stats.EquipmentAttackSpeed += AttackSpeedBonus;
			stats.EquipmentCritChance += CritChanceBonus;
			stats.EquipmentCritMultiplier += CritMultiplierBonus;
			stats.EquipmentHealthRegen += HealthRegenBonus;
			stats.EquipmentLifeSteal += LifeStealBonus;
			stats.EquipmentDodgeChance += DodgeChanceBonus;
		}

		// Remove this equipment's bonuses from player stats
		public void RemoveFrom(PlayerStats stats) {
			stats.EquipmentMaxHealth -= MaxHealthBonus;
			stats.EquipmentAttackDamage -= AttackDamageBonus;
			stats.EquipmentSpeed -= SpeedBonus;
			stats.EquipmentDefense -= DefenseBonus;
			stats.EquipmentAttackSpeed -= AttackSpeedBonus;
			stats.EquipmentCritChance -= CritChanceBonus;
			stats.EquipmentCritMultiplier -= CritMultiplierBonus;
			stats.EquipmentHealthRegen -= HealthRegenBonus;
			stats.EquipmentLifeSteal -= LifeStealBonus;
			stats.EquipmentDodgeChance -= DodgeChanceBonus;
		}

		// Get a formatted tooltip string for this equipment
		public string GetTooltip() {
			string tooltip = $"{Name}\n{Rarity}\n\n{Description}\n";

			if(RequiredLevel > 1)
				tooltip += $"Requires Level {RequiredLevel}\n";

			tooltip += "\nStats:\n";

			if(MaxHealthBonus != 0)
				tooltip += $"+{MaxHealthBonus} Max Health\n";
			if(AttackDamageBonus != 0)
				tooltip += $"+{AttackDamageBonus} Attack Damage\n";
			if(SpeedBonus != 0)
				tooltip += $"+{SpeedBonus:F0} Speed\n";
			if(DefenseBonus != 0)
				tooltip += $"+{DefenseBonus} Defense\n";
			if(AttackSpeedBonus != 0)
				tooltip += $"+{AttackSpeedBonus:F1} Attack Speed\n";
			if(CritChanceBonus != 0)
				tooltip += $"+{(CritChanceBonus * 100):F1}% Crit Chance\n";
			if(CritMultiplierBonus != 0)
				tooltip += $"+{CritMultiplierBonus:F1}x Crit Multiplier\n";
			if(HealthRegenBonus != 0)
				tooltip += $"+{HealthRegenBonus:F1} HP/sec Regen\n";
			if(LifeStealBonus != 0)
				tooltip += $"+{(LifeStealBonus * 100):F1}% Life Steal\n";
			if(DodgeChanceBonus != 0)
				tooltip += $"+{(DodgeChanceBonus * 100):F1}% Dodge\n";

			return tooltip;
		}
	}

	// Example equipment factory
	public static class EquipmentFactory {
		public static Equipment CreateIronSword() {
			var sword = new Equipment("Iron Sword", EquipmentSlot.Weapon, EquipmentRarity.Common) {
				ItemId = "iron_sword",
				Description = "A basic iron sword",
				AttackDamageBonus = 10,
				RequiredLevel = 1
			};
			return sword;
		}

		public static Equipment CreateVampireBlade() {
			var blade = new Equipment("Vampire Blade", EquipmentSlot.Weapon, EquipmentRarity.Epic) {
				ItemId = "vampire_blade",
				Description = "Drains the life from enemies",
				AttackDamageBonus = 25,
				LifeStealBonus = 0.15f,
				RequiredLevel = 10
			};
			return blade;
		}

		public static Equipment CreateLeatherArmor() {
			var armor = new Equipment("Leather Armor", EquipmentSlot.Armor, EquipmentRarity.Common) {
				ItemId = "leather_armor",
				Description = "Basic leather protection",
				MaxHealthBonus = 20,
				DefenseBonus = 5,
				RequiredLevel = 1
			};
			return armor;
		}

		public static Equipment CreateSpeedBoots() {
			var boots = new Equipment("Boots of Swiftness", EquipmentSlot.Boots, EquipmentRarity.Rare) {
				ItemId = "speed_boots",
				Description = "Light on your feet",
				SpeedBonus = 50f,
				DodgeChanceBonus = 0.05f,
				RequiredLevel = 5
			};
			return boots;
		}

		public static Equipment CreateCriticalRing() {
			var ring = new Equipment("Ring of Precision", EquipmentSlot.Ring, EquipmentRarity.Rare) {
				ItemId = "critical_ring",
				Description = "Enhances critical strikes",
				CritChanceBonus = 0.10f,
				CritMultiplierBonus = 0.5f,
				RequiredLevel = 8
			};
			return ring;
		}

		public static Equipment CreateRegenerationAmulet() {
			var amulet = new Equipment("Amulet of Vitality", EquipmentSlot.Amulet, EquipmentRarity.Uncommon) {
				ItemId = "regeneration_amulet",
				Description = "Slowly restores health",
				MaxHealthBonus = 30,
				HealthRegenBonus = 2f,
				RequiredLevel = 3
			};
			return amulet;
		}

		public static Equipment CreateLegendarySword() {
			var sword = new Equipment("Excalibur", EquipmentSlot.Weapon, EquipmentRarity.Legendary) {
				ItemId = "excalibur",
				Description = "The legendary sword of heroes",
				AttackDamageBonus = 50,
				AttackSpeedBonus = 0.5f,
				CritChanceBonus = 0.15f,
				CritMultiplierBonus = 1f,
				RequiredLevel = 20
			};
			return sword;
		}


		public static Equipment CreateFromId(string itemId) {
			return itemId switch {
				"iron_sword" => CreateIronSword(),
				"vampire_blade" => CreateVampireBlade(),
				"leather_armor" => CreateLeatherArmor(),
				"speed_boots" => CreateSpeedBoots(),
				"critical_ring" => CreateCriticalRing(),
				"regeneration_amulet" => CreateRegenerationAmulet(),
				"excalibur" => CreateLegendarySword(),
				_ => null
			};
		}
	}
}