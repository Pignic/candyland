using EldmeresTale.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class EquipmentFactory {
	private static Dictionary<string, EquipmentDefinition> _catalog;
	private static bool _initialized = false;

	public static Dictionary<string, EquipmentDefinition> Catalog {
		get {
			if (!_initialized) {
				Initialize();
			}

			return _catalog;
		}
	}

	// ===== INITIALIZATION =====

	public static void Initialize(string path = "Assets/Data/equipment.json") {
		_catalog = new Dictionary<string, EquipmentDefinition>();

		try {
			if (!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[EQUIPMENT FACTORY] File not found: {path}");
				return;
			}

			string json = File.ReadAllText(path);
			EquipmentCatalogData data = JsonSerializer.Deserialize<EquipmentCatalogData>(json);

			if (data?.equipment == null) {
				System.Diagnostics.Debug.WriteLine("[EQUIPMENT FACTORY] Invalid JSON format");
				return;
			}

			foreach (EquipmentDefinition item in data.equipment) {
				_catalog[item.Id] = item;
			}

			System.Diagnostics.Debug.WriteLine($"[EQUIPMENT FACTORY] Loaded {_catalog.Count} items from {path}");

		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[EQUIPMENT FACTORY] Error: {ex.Message}");
		}

		_initialized = true;
	}

	public static Equipment CreateFromId(string itemId) {
		if (!_initialized) {
			Initialize();
		}

		if (!_catalog.ContainsKey(itemId)) {
			System.Diagnostics.Debug.WriteLine($"[EQUIPMENT FACTORY] Item not found: {itemId}");
			return null;
		}

		EquipmentDefinition def = _catalog[itemId];

		var equipment = new Equipment(def.Name, def.Slot, def.Rarity) {
			ItemId = def.Id,
			Description = def.Description,
			RequiredLevel = def.RequiredLevel,

			// Combat stats
			AttackDamageBonus = def.AttackDamage,
			DefenseBonus = def.Defense,
			MaxHealthBonus = def.MaxHealth,

			// Advanced combat
			AttackSpeedBonus = def.AttackSpeed,
			CritChanceBonus = def.CritChance,
			CritMultiplierBonus = def.CritMultiplier,
			LifeStealBonus = def.LifeSteal,
			DodgeChanceBonus = def.DodgeChance,

			// Regeneration
			HealthRegenBonus = def.HealthRegen,

			// Movement
			SpeedBonus = def.Speed
		};

		return equipment;
	}

	public static List<string> GetItemsBySlot(EquipmentSlot slot) {
		if (!_initialized) {
			Initialize();
		}

		var items = new List<string>();
		foreach (KeyValuePair<string, EquipmentDefinition> kvp in _catalog) {
			if (kvp.Value.Slot == slot) {
				items.Add(kvp.Key);
			}
		}
		return items;
	}

	public static List<string> GetItemsByRarity(EquipmentRarity rarity) {
		if (!_initialized) {
			Initialize();
		}

		var items = new List<string>();
		foreach (KeyValuePair<string, EquipmentDefinition> kvp in _catalog) {
			if (kvp.Value.Rarity == rarity) {
				items.Add(kvp.Key);
			}
		}
		return items;
	}

	// JSON container class
	private class EquipmentCatalogData {
		public List<EquipmentDefinition> equipment { get; set; }
	}
}