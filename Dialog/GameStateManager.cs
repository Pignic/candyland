using EldmeresTale.Core;
using EldmeresTale.Entities;
using System.Collections.Generic;

namespace EldmeresTale.Dialog;

public class GameStateManager {

	// Quest tracking
	private Dictionary<string, QuestStatus> quests;

	// Item inventory 
	private Inventory inventory;

	// Game flags
	private Dictionary<string, bool> flags;

	// Current room
	private string currentRoom;

	// NPC dialog tree overrides
	private Dictionary<string, string> npcDialogTrees;

	// Time state
	private bool isDay = true;

	public GameStateManager(Player player) {
		quests = new Dictionary<string, QuestStatus>();
		inventory = player.Inventory;
		flags = new Dictionary<string, bool>();
		npcDialogTrees = new Dictionary<string, string>();
		currentRoom = "";
	}

	public void startQuest(string questId) {
		quests[questId] = QuestStatus.Active;
		System.Diagnostics.Debug.WriteLine($"Quest started: {questId}");
	}

	public void completeQuest(string questId) {
		quests[questId] = QuestStatus.Completed;
		System.Diagnostics.Debug.WriteLine($"Quest completed: {questId}");
	}

	public void failQuest(string questId) {
		quests[questId] = QuestStatus.Failed;
		System.Diagnostics.Debug.WriteLine($"Quest failed: {questId}");
	}

	public bool checkQuestStatus(string questId, string status) {
		if (!quests.ContainsKey(questId)) {
			// Quest not started
			return status == "not_started" || status == "!started";
		}

		QuestStatus questStatus = quests[questId];

		return status.ToLower() switch {
			"active" => questStatus == QuestStatus.Active,
			"completed" => questStatus == QuestStatus.Completed,
			"failed" => questStatus == QuestStatus.Failed,
			"started" => questStatus != QuestStatus.NotStarted,
			"not_started" => questStatus == QuestStatus.NotStarted,
			_ => false
		};
	}

	public void giveItem(string itemId, int count) {
		inventory.AddItem(itemId, count);
		System.Diagnostics.Debug.WriteLine($"Item given: {itemId} x{count}");
	}

	public void removeItem(string itemId, int count) {
		inventory.AddItem(itemId, count);
		System.Diagnostics.Debug.WriteLine($"Item removed: {itemId} x{count}");
	}

	public bool hasItem(string itemId) {
		return inventory.GetItemCount(itemId) > 0;
	}

	public int getItemCount(string itemId) {
		return inventory.GetItemCount(itemId);
	}

	public void setFlag(string flagName, bool value) {
		flags[flagName] = value;
		System.Diagnostics.Debug.WriteLine($"Flag set: {flagName} = {value}");
	}

	public bool getFlag(string flagName) {
		return flags.ContainsKey(flagName) && flags[flagName];
	}

	public Dictionary<string, bool> getFlags() {
		return flags;
	}

	public void setCurrentRoom(string roomId) {
		currentRoom = roomId;
	}

	public string getCurrentRoom() {
		return currentRoom;
	}

	public void travelToRoom(string roomId) {
		currentRoom = roomId;
		System.Diagnostics.Debug.WriteLine($"Traveled to room: {roomId}");
		// In actual implementation, this would trigger room transition in game
	}

	public void setDayNight(bool isDay) {
		this.isDay = isDay;
	}

	public bool checkTime(string timeCheck) {
		return timeCheck.ToLower() switch {
			"is_day" => isDay,
			"is_night" => !isDay,
			_ => true
		};
	}

	public void unlockDoor(string doorId) {
		setFlag($"door_{doorId}_unlocked", true);
		System.Diagnostics.Debug.WriteLine($"Door unlocked: {doorId}");
	}

	public void lockDoor(string doorId) {
		setFlag($"door_{doorId}_unlocked", false);
		System.Diagnostics.Debug.WriteLine($"Door locked: {doorId}");
	}

	public void spawnNPC(string npcId) {
		setFlag($"npc_{npcId}_spawned", true);
		System.Diagnostics.Debug.WriteLine($"NPC spawned: {npcId}");
		// In actual implementation, this would add NPC to current room
	}

	public void despawnNPC(string npcId) {
		setFlag($"npc_{npcId}_spawned", false);
		System.Diagnostics.Debug.WriteLine($"NPC despawned: {npcId}");
		// In actual implementation, this would remove NPC from current room
	}

	public void setNPCDialogTree(string npcId, string treeId) {
		npcDialogTrees[npcId] = treeId;
		System.Diagnostics.Debug.WriteLine($"NPC dialog changed: {npcId} -> {treeId}");
	}

	public string getNPCDialogTree(string npcId) {
		return npcDialogTrees.ContainsKey(npcId) ? npcDialogTrees[npcId] : null;
	}
}

public enum QuestStatus {
	NotStarted,
	Active,
	Completed,
	Failed
}