using EldmeresTale.Entities;
using EldmeresTale.Quests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;

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
		if(!Directory.Exists(SAVE_FOLDER)) {
			Directory.CreateDirectory(SAVE_FOLDER);
			System.Diagnostics.Debug.WriteLine($"[SAVE] Created saves folder: {SAVE_FOLDER}");
		}
	}

	// ================================================================
	// SAVE
	// ================================================================

	/// <summary>
	/// Save the current game state
	/// </summary>
	public bool Save(GameServices gameState, string saveName = "save1") {
		try {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Starting save to: {saveName}");

			SaveData saveData = CreateSaveData(gameState);
			saveData.SaveName = saveName;
			saveData.SaveTime = DateTime.Now;

			string json = JsonSerializer.Serialize(saveData, JsonOptions);
			string filepath = GetSaveFilePath(saveName);

			File.WriteAllText(filepath, json);

			System.Diagnostics.Debug.WriteLine($"[SAVE] Successfully saved to: {filepath}");
			return true;

		} catch(Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Error saving game: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[SAVE] Stack trace: {ex.StackTrace}");
			return false;
		}
	}

	private SaveData CreateSaveData(GameServices gameState) {
		var saveData = new SaveData();

		System.Diagnostics.Debug.WriteLine("[SAVE] Saving player data...");
		saveData.Player = SavePlayer(gameState.Player);

		System.Diagnostics.Debug.WriteLine("[SAVE] Saving quest data...");
		saveData.Quests = SaveQuests(gameState.QuestManager);

		System.Diagnostics.Debug.WriteLine("[SAVE] Saving world data...");
		saveData.World = SaveWorld(gameState);

		return saveData;
	}

	// ================================================================
	// SAVE - PLAYER
	// ================================================================

	private PlayerSaveData SavePlayer(Player player) {
		var data = new PlayerSaveData {
			// Position
			X = player.Position.X,
			Y = player.Position.Y,

			// Core stats
			Health = player.health,
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
		foreach(var item in player.Inventory.Items) {
			if(item is Equipment equip) {
				data.Inventory.Add(SaveEquipment(equip));
			}
		}

		// Save equipped items
		foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) {
			var equipped = player.Inventory.GetEquippedItem(slot);
			if(equipped != null) {
				data.EquippedItems[slot.ToString()] = SaveEquipment(equipped);
			}
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Saved {data.Inventory.Count} items, {data.EquippedItems.Count} equipped");

		return data;
	}

	private EquipmentSaveData SaveEquipment(Equipment equip) {
		return new EquipmentSaveData {
			ItemId = equip.ItemId,
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

	// ================================================================
	// SAVE - QUESTS (Placeholder for now)
	// ================================================================

	private QuestSaveData SaveQuests(QuestManager questManager) {
		var data = new QuestSaveData();

		// Save completed quests
		data.CompletedQuests = new List<string>(questManager.GetCompletedQuests());

		// Save active quests
		foreach(var instance in questManager.getActiveQuests()) {
			var activeQuestData = new ActiveQuestData {
				QuestId = instance.quest.id,
				CurrentNodeId = instance.currentNodeId,
				ObjectiveProgress = new Dictionary<string, int>()
			};

			// Save objective progress
			// Convert QuestObjective keys to string keys (type:target format)
			foreach(var kvp in instance.objectiveProgress) {
				string key = $"{kvp.Key.type}:{kvp.Key.target}";
				activeQuestData.ObjectiveProgress[key] = kvp.Value;
			}

			data.ActiveQuests.Add(activeQuestData);
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Saved {data.ActiveQuests.Count} active quests, {data.CompletedQuests.Count} completed");

		return data;
	}

	// ================================================================
	// SAVE - WORLD
	// ================================================================

	private WorldSaveData SaveWorld(GameServices gameState) {
		var data = new WorldSaveData {
			CurrentRoomId = gameState.RoomManager.currentRoom?.id ?? "room1"
		};

		// Copy game flags
		foreach(var kvp in gameState.GameState.getFlags()) {
			data.GameFlags[kvp.Key] = kvp.Value.ToString();
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Saved current room: {data.CurrentRoomId}, {data.GameFlags.Count} flags");

		return data;
	}

	// ================================================================
	// LOAD
	// ================================================================

	/// <summary>
	/// Load a saved game state
	/// </summary>
	public bool Load(GameServices gameState, string saveName = "save1") {
		string filepath = GetSaveFilePath(saveName);

		if(!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Save file not found: {filepath}");
			return false;
		}

		try {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Loading save from: {filepath}");

			string json = File.ReadAllText(filepath);
			SaveData saveData = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);

			if(saveData == null) {
				System.Diagnostics.Debug.WriteLine("[SAVE] Failed to deserialize save data");
				return false;
			}

			System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded save version {saveData.Version} from {saveData.SaveTime}");

			ApplySaveData(gameState, saveData);

			System.Diagnostics.Debug.WriteLine("[SAVE] Successfully loaded game");
			return true;

		} catch(Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Error loading save: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[SAVE] Stack trace: {ex.StackTrace}");
			return false;
		}
	}

	private void ApplySaveData(GameServices gameState, SaveData saveData) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Applying player data...");
		LoadPlayer(gameState.Player, saveData.Player);

		System.Diagnostics.Debug.WriteLine("[SAVE] Applying quest data...");
		LoadQuests(gameState.QuestManager, saveData.Quests);

		System.Diagnostics.Debug.WriteLine("[SAVE] Applying world data...");
		LoadWorld(gameState, saveData.World);
	}

	// ================================================================
	// LOAD - PLAYER
	// ================================================================

	private void LoadPlayer(Player player, PlayerSaveData data) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Loading player data...");

		// Position
		player.Position = new Vector2(data.X, data.Y);

		// Core stats
		player.health = data.Health;
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
		foreach(var equipData in data.Inventory) {
			Equipment equip = LoadEquipment(equipData);
			if(equip != null) {
				player.Inventory.AddItem(equip);
			}
		}

		// Load and equip items
		foreach(var kvp in data.EquippedItems) {
			if(!Enum.TryParse<EquipmentSlot>(kvp.Key, out EquipmentSlot slot)) {
				System.Diagnostics.Debug.WriteLine($"[SAVE] Unknown equipment slot: {kvp.Key}");
				continue;
			}

			// Find the item in inventory by ItemId
			var itemInInventory = player.Inventory.Items
				.OfType<Equipment>()
				.FirstOrDefault(e => e.ItemId == kvp.Value.ItemId && e.Slot == slot);

			if(itemInInventory != null) {
				player.Inventory.Equip(itemInInventory, player.Stats);
				System.Diagnostics.Debug.WriteLine($"[SAVE] Equipped {itemInInventory.Name} in {slot}");
			} else {
				System.Diagnostics.Debug.WriteLine($"[SAVE] Could not find item to equip in {slot}: {kvp.Value.ItemId}");
			}
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded player at ({data.X}, {data.Y}), Level {data.Level}, HP: {data.Health}/{data.MaxHealth}");
		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded {data.Inventory.Count} items, {data.EquippedItems.Count} equipped");
	}

	private Equipment LoadEquipment(EquipmentSaveData data) {
		// Try to create from factory using ItemId
		Equipment equip = EquipmentFactory.CreateFromId(data.ItemId);

		if(equip != null) {
			// Item created successfully from factory
			return equip;
		}

		// Fallback: Manually recreate equipment (in case ItemId not found in factory)
		System.Diagnostics.Debug.WriteLine($"[SAVE] WARNING: Item not found in factory: {data.ItemId}, creating manually");

		if(!Enum.TryParse<EquipmentSlot>(data.Slot, out EquipmentSlot slot)) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] ERROR: Invalid equipment slot: {data.Slot}");
			return null;
		}

		if(!Enum.TryParse<EquipmentRarity>(data.Rarity, out EquipmentRarity rarity)) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] ERROR: Invalid rarity: {data.Rarity}");
			return null;
		}

		equip = new Equipment(data.Name, slot, rarity) {
			ItemId = data.ItemId,
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

		return equip;
	}

	// ================================================================
	// LOAD - QUESTS (Placeholder for now)
	// ================================================================

	private void LoadQuests(QuestManager questManager, QuestSaveData data) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Loading quest data...");

		// Clear current quest state
		questManager.ClearAll();

		// Load completed quests
		foreach(var questId in data.CompletedQuests) {
			questManager.MarkAsCompleted(questId);
		}

		// Load active quests
		foreach(var activeQuest in data.ActiveQuests) {
			questManager.LoadQuest(
				activeQuest.QuestId,
				activeQuest.CurrentNodeId,
				activeQuest.ObjectiveProgress
			);
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded {data.ActiveQuests.Count} active quests, {data.CompletedQuests.Count} completed");
	}

	// ================================================================
	// LOAD - WORLD
	// ================================================================

	private void LoadWorld(GameServices gameState, WorldSaveData data) {
		System.Diagnostics.Debug.WriteLine("[SAVE] Loading world data...");

		// Load current room
		if(!string.IsNullOrEmpty(data.CurrentRoomId)) {
			gameState.RoomManager.setCurrentRoom(data.CurrentRoomId);
			System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded room: {data.CurrentRoomId}");
		}

		// Load game flags
		gameState.GameState.getFlags().Clear();
		foreach(var kvp in data.GameFlags) {
			gameState.GameState.getFlags()[kvp.Key] = bool.Parse(kvp.Value);
		}

		System.Diagnostics.Debug.WriteLine($"[SAVE] Loaded {data.GameFlags.Count} game flags");
	}

	// ================================================================
	// UTILITY
	// ================================================================

	/// <summary>
	/// Check if a save file exists
	/// </summary>
	public bool SaveExists(string saveName = "save1") {
		return File.Exists(GetSaveFilePath(saveName));
	}

	/// <summary>
	/// Get list of all save file names
	/// </summary>
	public List<string> GetSaveFiles() {
		if(!Directory.Exists(SAVE_FOLDER)) {
			return new List<string>();
		}

		return Directory.GetFiles(SAVE_FOLDER, "*" + SAVE_EXTENSION)
			.Select(Path.GetFileNameWithoutExtension)
			.ToList();
	}

	/// <summary>
	/// Delete a save file
	/// </summary>
	public bool DeleteSave(string saveName) {
		try {
			string filepath = GetSaveFilePath(saveName);
			if(File.Exists(filepath)) {
				File.Delete(filepath);
				System.Diagnostics.Debug.WriteLine($"[SAVE] Deleted save: {filepath}");
				return true;
			}
			return false;
		} catch(Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SAVE] Error deleting save: {ex.Message}");
			return false;
		}
	}

	private string GetSaveFilePath(string saveName) {
		return Path.Combine(SAVE_FOLDER, saveName + SAVE_EXTENSION);
	}
}