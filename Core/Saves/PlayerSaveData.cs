using System.Collections.Generic;

namespace Candyland.Core.Saves;

public class PlayerSaveData {
	// ================================================================
	// POSITION
	// ================================================================

	public float X { get; set; }
	public float Y { get; set; }

	// ================================================================
	// CORE STATS
	// ================================================================

	public int Health { get; set; }
	public int Level { get; set; }
	public int XP { get; set; }
	public int Coins { get; set; }

	// ================================================================
	// BASE STATS (from PlayerStats)
	// ================================================================

	public int MaxHealth { get; set; }
	public int AttackDamage { get; set; }
	public int Defense { get; set; }
	public float Speed { get; set; }
	public float AttackSpeed { get; set; }
	public float CritChance { get; set; }
	public float CritMultiplier { get; set; }
	public float HealthRegen { get; set; }
	public float LifeSteal { get; set; }
	public float DodgeChance { get; set; }

	// ================================================================
	// INVENTORY
	// ================================================================

	/// <summary>
	/// All items in inventory (unequipped + equipped)
	/// </summary>
	public List<EquipmentSaveData> Inventory { get; set; }

	/// <summary>
	/// Currently equipped items by slot name
	/// Key: "Weapon", "Armor", "Helmet", etc.
	/// Value: The equipped item data
	/// </summary>
	public Dictionary<string, EquipmentSaveData> EquippedItems { get; set; }

	public PlayerSaveData() {
		Inventory = new List<EquipmentSaveData>();
		EquippedItems = new Dictionary<string, EquipmentSaveData>();
	}
}