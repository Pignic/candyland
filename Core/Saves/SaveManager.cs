using EldmeresTale.Entities;
using EldmeresTale.Entities.Factories;
using EldmeresTale.Quests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EldmeresTale.Core.Saves;

public class SaveManager {
	private const string SAVE_FOLDER = "Saves";
	private const string SAVE_EXTENSION = ".json";

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
		WriteIndented = true,  // Pretty print for debugging
		PropertyNameCaseInsensitive = true
	};

	public SaveManager() {
		// Create saves folder if it doesn't exist
		if (!Directory.Exists(SAVE_FOLDER)) {
			Directory.CreateDirectory(SAVE_FOLDER);
			System.Diagnostics.Debug.WriteLine($"[SAVE] Created saves folder: {SAVE_FOLDER}");
		}
	}

	public bool Save(GameServices gameServices, string saveName = "save1") {
		try {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Starting save to: {saveName}");

			SaveData saveData = CreateSaveData(gameServices);
			saveData.SaveName = saveName;
			saveData.SaveTime = DateTime.Now;

			string json = JsonSerializer.Serialize(saveData, JsonOptions);
			string filepath = GetSaveFilePath(saveName);

			File.WriteAllText(filepath, json);

			System.Diagnostics.Debug.WriteLine($"[SAVE] Successfully saved to: {filepath}");
			return true;

		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Error saving game: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[SAVE] Stack trace: {ex.StackTrace}");
			return false;
		}
	}

	private static SaveData CreateSaveData(GameServices gameState) {
		SaveData saveData = new SaveData();

		System.Diagnostics.Debug.WriteLine("[SAVE] Saving player data...");
		saveData.Player = SavePlayer(gameState.Player);

		System.Diagnostics.Debug.WriteLine("[SAVE] Saving quest data...");
		saveData.Quests = SaveQuests(gameState.QuestManager);

		System.Diagnostics.Debug.WriteLine("[SAVE] Saving world data...");
		saveData.World = SaveWorld(gameState);

		return saveData;
	}

	private static PlayerSaveData SavePlayer(Player player) {
		PlayerSaveData data = new PlayerSaveData {
			// Position
			X = player.Position.X,
			Y = player.Position.Y,

			// Core stats
			Health = player.Health,
			Level = player.Level,
			XP = player.XP,
			Coins = player.Coins,

			// Base stats
			MaxHealth = player.Stats.BaseMaxHealth,
			AttackDamage = player.Stats.BaseAttackDamage,
			Defense = player.Stats.BaseDefense,
			Speed = player.Stats.BaseSpeed,
			AttackSpeed = player.Stats.BaseAttackSpeed,
			CritChance = player.Stats.BaseCritChance,
			CritMultiplier = player.Stats.BaseCritMultiplier,
			HealthRegen = player.Stats.BaseHealthRegen,
			LifeSteal = player.Stats.BaseLifeSteal,
		};

		// Save inventory items
		foreach (Equipment item in player.Inventory.EquipmentItems) {
			if (item is Equipment equip) {
				data.Inventory.Add(SaveEquipment(equip));
			}
		}

		// Save equipped items
		foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>()) {
			Equipment equipped = player.Inventory.GetEquippedItem(slot);
			if (equipped != null) {
				data.EquippedItems[slot.ToString()] = SaveEquipment(equipped);
			}
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Saved {data.Inventory.Count} items, {data.EquippedItems.Count} equipped");

		return data;
	}

	private static EquipmentSaveData SaveEquipment(Equipment equip) {
		return new EquipmentSaveData {
			ItemId = equip.EquipmentId,
			Name = equip.Name,
			Description = equip.Description,
			Slot = equip.Slot.ToString(),
			Rarity = equip.Rarity.ToString(),
			RequiredLevel = equip.RequiredLevel,

			// Stat bonuses
			AttackDamageBonus = equip.AttackDamageBonus,
			DefenseBonus = equip.DefenseBonus,
			MaxHealthBonus = equip.MaxHealthBonus,
			AttackSpeedBonus = equip.AttackSpeedBonus,
			CritChanceBonus = equip.CritChanceBonus,
			CritMultiplierBonus = equip.CritMultiplierBonus,
			LifeStealBonus = equip.LifeStealBonus,
			DodgeChanceBonus = equip.DodgeChanceBonus,
			HealthRegenBonus = equip.HealthRegenBonus,
			SpeedBonus = equip.SpeedBonus
		};
	}

	private static QuestSaveData SaveQuests(QuestManager questManager) {
		QuestSaveData data = new QuestSaveData {
			// Save completed quests
			CompletedQuests = [.. questManager.GetCompletedQuests()]
		};

		// Save active quests
		foreach (QuestInstance instance in questManager.GetActiveQuests()) {
			ActiveQuestData activeQuestData = new ActiveQuestData {
				QuestId = instance.Quest.Id,
				CurrentNodeId = instance.CurrentNodeId,
				ObjectiveProgress = []
			};

			// Save objective progress
			// Convert QuestObjective keys to string keys (type:target format)
			foreach (KeyValuePair<QuestObjective, int> kvp in instance.ObjectiveProgress) {
				string key = $"{kvp.Key.Type}:{kvp.Key.Target}";
				activeQuestData.ObjectiveProgress[key] = kvp.Value;
			}

			data.ActiveQuests.Add(activeQuestData);
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Saved {data.ActiveQuests.Count} active quests, {data.CompletedQuests.Count} completed");

		return data;
	}

	private static WorldSaveData SaveWorld(GameServices gameServices) {
		WorldSaveData data = new WorldSaveData {
			CurrentRoomId = gameServices.RoomManager.CurrentRoom?.Id ?? "room1"
		};

		// Copy game flags
		foreach (KeyValuePair<string, bool> kvp in gameServices.GameState.GetFlags()) {
			data.GameFlags[kvp.Key] = kvp.Value.ToString();
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Saved current room: {data.CurrentRoomId}, {data.GameFlags.Count} flags");

		return data;
	}

	public SaveData Load(string saveName = "save1") {
		string filepath = GetSaveFilePath(saveName);

		if (!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Save file not found: {filepath}");
			return null;
		}

		try {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Loading save from: {filepath}");

			string json = File.ReadAllText(filepath);
			SaveData saveData = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);

			if (saveData == null) {
				System.Diagnostics.Debug.WriteLine("[SAVE] Failed to deserialize save data");
				return saveData;
			}

			System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded save version {saveData.Version} from {saveData.SaveTime}");


			System.Diagnostics.Debug.WriteLine("[SAVE] Successfully loaded game");
			return saveData;

		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Error loading save: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[SAVE] Stack trace: {ex.StackTrace}");
			return null;
		}
	}

	public static void ApplySaveData(GameServices gameServices, SaveData saveData) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Applying player data...");
		LoadPlayer(gameServices.Player, saveData.Player);

		System.Diagnostics.Debug.WriteLine("[SAVE] Applying quest data...");
		LoadQuests(gameServices.QuestManager, saveData.Quests);

		System.Diagnostics.Debug.WriteLine("[SAVE] Applying world data...");
		string roomId = LoadWorld(gameServices, saveData.World);
		gameServices.RoomManager.TransitionToRoom(roomId, gameServices.Player);
	}

	private static void LoadPlayer(Player player, PlayerSaveData data) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Loading player data...");

		// Position
		player.Position = new Microsoft.Xna.Framework.Vector2(data.X, data.Y);

		// Core stats
		player.Health = data.Health;
		player.Level = data.Level;
		player.XP = data.XP;
		player.Coins = data.Coins;

		// Clear inventory first (this also unequips everything)
		player.Inventory.Clear();

		// Load base stats
		player.Stats.BaseMaxHealth = data.MaxHealth;
		player.Stats.BaseAttackDamage = data.AttackDamage;
		player.Stats.BaseDefense = data.Defense;
		player.Stats.BaseSpeed = data.Speed;
		player.Stats.BaseAttackSpeed = data.AttackSpeed;
		player.Stats.BaseCritChance = data.CritChance;
		player.Stats.BaseCritMultiplier = data.CritMultiplier;
		player.Stats.BaseHealthRegen = data.HealthRegen;
		player.Stats.BaseLifeSteal = data.LifeSteal;
		player.Stats.BaseDodgeChance = data.DodgeChance;

		// Load inventory items
		foreach (EquipmentSaveData equipData in data.Inventory) {
			Equipment equip = LoadEquipment(equipData);
			if (equip != null) {
				player.Inventory.AddItem(equip);
			}
		}

		// Load and equip items
		foreach (KeyValuePair<string, EquipmentSaveData> kvp in data.EquippedItems) {
			if (!Enum.TryParse<EquipmentSlot>(kvp.Key, out EquipmentSlot slot)) {
				System.Diagnostics.Debug.WriteLine($"[SAVE] Unknown equipment slot: {kvp.Key}");
				continue;
			}

			// Find the item in inventory by ItemId
			Equipment itemInInventory = player.Inventory.EquipmentItems
				.OfType<Equipment>()
				.FirstOrDefault(e => e.EquipmentId == kvp.Value.ItemId && e.Slot == slot);

			if (itemInInventory != null) {
				player.Inventory.Equip(itemInInventory, player.Stats);
				System.Diagnostics.Debug.WriteLine($"[SAVE] Equipped {itemInInventory.Name} in {slot}");
			} else {
				System.Diagnostics.Debug.WriteLine($"[SAVE] Could not find item to equip in {slot}: {kvp.Value.ItemId}");
			}
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded player at ({data.X}, {data.Y}), Level {data.Level}, HP: {data.Health}/{data.MaxHealth}");
		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded {data.Inventory.Count} items, {data.EquippedItems.Count} equipped");
	}

	private static Equipment LoadEquipment(EquipmentSaveData data) {
		// Try to create from factory using ItemId
		Equipment equip = EquipmentFactory.CreateFromId(data.ItemId);

		if (equip != null) {
			// Item created successfully from factory
			return equip;
		}

		// Fallback: Manually recreate equipment (in case ItemId not found in factory)
		System.Diagnostics.Debug.WriteLine($"[SAVE] WARNING: Item not found in factory: {data.ItemId}, creating manually");

		if (!Enum.TryParse<EquipmentSlot>(data.Slot, out EquipmentSlot slot)) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] ERROR: Invalid equipment slot: {data.Slot}");
			return null;
		}

		if (!Enum.TryParse<EquipmentRarity>(data.Rarity, out EquipmentRarity rarity)) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] ERROR: Invalid rarity: {data.Rarity}");
			return null;
		}

		return new Equipment(data.Name, slot, rarity) {
			EquipmentId = data.ItemId,
			Description = data.Description,
			RequiredLevel = data.RequiredLevel,

			// Stat bonuses
			AttackDamageBonus = data.AttackDamageBonus,
			DefenseBonus = data.DefenseBonus,
			MaxHealthBonus = data.MaxHealthBonus,
			AttackSpeedBonus = data.AttackSpeedBonus,
			CritChanceBonus = data.CritChanceBonus,
			CritMultiplierBonus = data.CritMultiplierBonus,
			LifeStealBonus = data.LifeStealBonus,
			DodgeChanceBonus = data.DodgeChanceBonus,
			HealthRegenBonus = data.HealthRegenBonus,
			SpeedBonus = data.SpeedBonus
		};
	}

	private static void LoadQuests(QuestManager questManager, QuestSaveData data) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Loading quest data...");

		// Clear current quest state
		questManager.ClearAll();

		// Load completed quests
		foreach (string questId in data.CompletedQuests) {
			questManager.MarkAsCompleted(questId);
		}

		// Load active quests
		foreach (ActiveQuestData activeQuest in data.ActiveQuests) {
			questManager.LoadQuest(
				activeQuest.QuestId,
				activeQuest.CurrentNodeId,
				activeQuest.ObjectiveProgress
			);
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded {data.ActiveQuests.Count} active quests, {data.CompletedQuests.Count} completed");
	}

	private static string LoadWorld(GameServices gameServices, WorldSaveData data) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Loading world data...");

		// Load game flags
		gameServices.GameState.GetFlags().Clear();
		foreach (KeyValuePair<string, string> kvp in data.GameFlags) {
			gameServices.GameState.GetFlags()[kvp.Key] = bool.Parse(kvp.Value);
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded {data.GameFlags.Count} game flags");
		return data.CurrentRoomId;
	}

	public bool SaveExists(string saveName = "save1") {
		return File.Exists(GetSaveFilePath(saveName));
	}

	public static List<string> GetSaveFiles() {
		if (!Directory.Exists(SAVE_FOLDER)) {
			return [];
		}

		return Directory.GetFiles(SAVE_FOLDER, "*" + SAVE_EXTENSION)
			.Select(Path.GetFileNameWithoutExtension)
			.ToList();
	}

	public bool DeleteSave(string saveName) {
		try {
			string filepath = GetSaveFilePath(saveName);
			if (File.Exists(filepath)) {
				File.Delete(filepath);
				System.Diagnostics.Debug.WriteLine($"[SAVE] Deleted save: {filepath}");
				return true;
			}
			return false;
		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Error deleting save: {ex.Message}");
			return false;
		}
	}

	private static string GetSaveFilePath(string saveName) {
		return Path.Combine(SAVE_FOLDER, saveName + SAVE_EXTENSION);
	}
}