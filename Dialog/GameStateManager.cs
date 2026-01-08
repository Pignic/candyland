using EldmeresTale.Core;
using EldmeresTale.Entities;
using System.Collections.Generic;

namespace EldmeresTale.Dialog;

public class GameStateManager {

	// Quest tracking
	private readonly Dictionary<string, QuestStatus> quests;

	// Item inventory 
	private readonly Inventory inventory;

	// Game flags
	private readonly Dictionary<string, bool> flags;

	// Current room
	private string currentRoom;

	// NPC dialog tree overrides
	private readonly Dictionary<string, string> npcDialogTrees;

	// Time state
	private bool isDay = true;

	public GameStateManager(Player player) {
		quests = [];
		inventory = player.Inventory;
		flags = [];
		npcDialogTrees = [];
		currentRoom = "";
	}

	public void StartQuest(string questId) {
		quests[questId] = QuestStatus.Active;
		System.Diagnostics.Debug.WriteLine($"Quest started: {questId}");
	}

	public void CompleteQuest(string questId) {
		quests[questId] = QuestStatus.Completed;
		System.Diagnostics.Debug.WriteLine($"Quest completed: {questId}");
	}

	public void FailQuest(string questId) {
		quests[questId] = QuestStatus.Failed;
		System.Diagnostics.Debug.WriteLine($"Quest failed: {questId}");
	}

	public bool CheckQuestStatus(string questId, string status) {
		if (!quests.TryGetValue(questId, out QuestStatus questStatus)) {
			// Quest not started
			return status == "not_started" || status == "!started";
		}

		return status.ToLower() switch {
			"active" => questStatus == QuestStatus.Active,
			"completed" => questStatus == QuestStatus.Completed,
			"failed" => questStatus == QuestStatus.Failed,
			"started" => questStatus != QuestStatus.NotStarted,
			"not_started" => questStatus == QuestStatus.NotStarted,
			_ => false
		};
	}

	public void GiveItem(string itemId, int count) {
		inventory.AddItem(itemId, count);
		System.Diagnostics.Debug.WriteLine($"Item given: {itemId} x{count}");
	}

	public void RemoveItem(string itemId, int count) {
		inventory.AddItem(itemId, count);
		System.Diagnostics.Debug.WriteLine($"Item removed: {itemId} x{count}");
	}

	public bool HasItem(string itemId) {
		return inventory.GetItemCount(itemId) > 0;
	}

	public int GetItemCount(string itemId) {
		return inventory.GetItemCount(itemId);
	}

	public void SetFlag(string flagName, bool value) {
		flags[flagName] = value;
		System.Diagnostics.Debug.WriteLine($"Flag set: {flagName} = {value}");
	}

	public bool GetFlag(string flagName) {
		return flags.ContainsKey(flagName) && flags[flagName];
	}

	public Dictionary<string, bool> GetFlags() {
		return flags;
	}

	public void SetCurrentRoom(string roomId) {
		currentRoom = roomId;
	}

	public string GetCurrentRoom() {
		return currentRoom;
	}

	public void TravelToRoom(string roomId) {
		currentRoom = roomId;
		System.Diagnostics.Debug.WriteLine($"Traveled to room: {roomId}");
		// In actual implementation, this would trigger room transition in game
	}

	public void SetDayNight(bool isDay) {
		this.isDay = isDay;
	}

	public bool CheckTime(string timeCheck) {
		return timeCheck.ToLower() switch {
			"is_day" => isDay,
			"is_night" => !isDay,
			_ => true
		};
	}

	public void UnlockDoor(string doorId) {
		SetFlag($"door_{doorId}_unlocked", true);
		System.Diagnostics.Debug.WriteLine($"Door unlocked: {doorId}");
	}

	public void LockDoor(string doorId) {
		SetFlag($"door_{doorId}_unlocked", false);
		System.Diagnostics.Debug.WriteLine($"Door locked: {doorId}");
	}

	public void SpawnNPC(string npcId) {
		SetFlag($"npc_{npcId}_spawned", true);
		System.Diagnostics.Debug.WriteLine($"NPC spawned: {npcId}");
		// In actual implementation, this would add NPC to current room
	}

	public void DespawnNPC(string npcId) {
		SetFlag($"npc_{npcId}_spawned", false);
		System.Diagnostics.Debug.WriteLine($"NPC despawned: {npcId}");
		// In actual implementation, this would remove NPC from current room
	}

	public void SetNPCDialogTree(string npcId, string treeId) {
		npcDialogTrees[npcId] = treeId;
		System.Diagnostics.Debug.WriteLine($"NPC dialog changed: {npcId} -> {treeId}");
	}

	public string GetNPCDialogTree(string npcId) {
		return npcDialogTrees.TryGetValue(npcId, out string value) ? value : null;
	}
}

public enum QuestStatus {
	NotStarted,
	Active,
	Completed,
	Failed
}