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

			if (RequiredLevel > 1) {
				tooltip += $"Requires Level {RequiredLevel}\n";
			}

			tooltip += "\nStats:\n";

			if (MaxHealthBonus != 0) {
				tooltip += $"+{MaxHealthBonus} Max Health\n";
			}

			if (AttackDamageBonus != 0) {
				tooltip += $"+{AttackDamageBonus} Attack Damage\n";
			}

			if (SpeedBonus != 0) {
				tooltip += $"+{SpeedBonus:F0} Speed\n";
			}

			if (DefenseBonus != 0) {
				tooltip += $"+{DefenseBonus} Defense\n";
			}

			if (AttackSpeedBonus != 0) {
				tooltip += $"+{AttackSpeedBonus:F1} Attack Speed\n";
			}

			if (CritChanceBonus != 0) {
				tooltip += $"+{CritChanceBonus * 100:F1}% Crit Chance\n";
			}

			if (CritMultiplierBonus != 0) {
				tooltip += $"+{CritMultiplierBonus:F1}x Crit Multiplier\n";
			}

			if (HealthRegenBonus != 0) {
				tooltip += $"+{HealthRegenBonus:F1} HP/sec Regen\n";
			}

			if (LifeStealBonus != 0) {
				tooltip += $"+{LifeStealBonus * 100:F1}% Life Steal\n";
			}

			if (DodgeChanceBonus != 0) {
				tooltip += $"+{DodgeChanceBonus * 100:F1}% Dodge\n";
			}

			return tooltip;
		}
	}
}