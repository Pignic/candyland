using EldmeresTale.Entities;
using EldmeresTale.Quests;
using System;

namespace EldmeresTale.Dialog;

public class EffectExecutor {
	private readonly Player _player;
	private readonly GameStateManager _gameState;
	private QuestManager _questManager;  // Set after initialization

	public EffectExecutor(Player player, GameStateManager gameState) {
		_player = player;
		_gameState = gameState;
	}

	public void SetQuestManager(QuestManager questManager) {
		_questManager = questManager;
	}

	public void execute(string effect) {
		if(string.IsNullOrEmpty(effect)) {
			return;
		}

		string[] tokens = effect.Split('.');
		if(tokens.Length < 2) {
			return;
		}

		switch(tokens[0]) {
			case "quest":
				executeQuest(tokens);
				break;
			case "item":
				executeItem(tokens);
				break;
			case "player":
				executePlayer(tokens);
				break;
			case "flag":
				executeFlag(tokens);
				break;
			case "door":
				executeDoor(tokens);
				break;
			case "room":
				executeRoom(tokens);
				break;
			case "npc":
				executeNpc(tokens);
				break;
			case "dialog":
				executeDialog(tokens);
				break;
			default:
				break;
		}
	}

	private void executeQuest(string[] tokens) {
		// Format: quest.action.quest_id
		if(tokens.Length < 3) {
			System.Diagnostics.Debug.WriteLine("[EFFECT] Invalid quest effect format");
			return;
		}

		string action = tokens[1];
		string questId = tokens[2];

		System.Diagnostics.Debug.WriteLine($"[EFFECT] Quest effect: {action} -> {questId}");

		switch(action) {
			case "start":
				// Use QuestManager if available
				if(_questManager != null) {
					System.Diagnostics.Debug.WriteLine($"[EFFECT] Calling questManager.startQuest({questId})");
					bool success = _questManager.StartQuest(questId);
					System.Diagnostics.Debug.WriteLine($"[EFFECT] Quest start result: {success}");
				} else {
					// Fallback to old GameState method
					_gameState.startQuest(questId);
					System.Diagnostics.Debug.WriteLine("[EFFECT] WARNING: QuestManager is null!");
				}
				break;
			case "complete":
				// Quest completion is automatic in QuestManager when objectives done
				// But keep this for manual completion or old GameState
				_gameState.completeQuest(questId);
				break;
			case "fail":
				_gameState.failQuest(questId);
				break;
		}
	}

	private void executeItem(string[] tokens) {
		// Format: item.action.item_id.count
		// Example: item.give.health_potion.3, item.remove.quest_item
		if(tokens.Length < 3) {
			return;
		}

		string action = tokens[1];
		string itemId = tokens[2];
		int count = tokens.Length >= 4 ? int.Parse(tokens[3]) : 1;

		switch(action) {
			case "give":
				_gameState.giveItem(itemId, count);
				break;
			case "remove":
				_gameState.removeItem(itemId, count);
				break;
		}
	}

	private void executePlayer(string[] tokens) {
		// Format: player.action.value
		if(tokens.Length < 3) {
			return;
		}

		string action = tokens[1];
		int value = int.Parse(tokens[2]);

		switch(action) {
			case "heal":
				_player.health = Math.Min(_player.health + value, _player.MaxHealth);
				break;
			case "damage":
				_player.health = Math.Max(_player.health - value, 0);
				break;
			case "xp":
				_player.GainXP(value);
				break;
		}
	}

	private void executeFlag(string[] tokens) {
		// Format: flag.action.flag_name
		if(tokens.Length < 3) {
			return;
		}

		string action = tokens[1];
		string flagName = tokens[2];

		switch(action) {
			case "set":
				_gameState.setFlag(flagName, true);
				break;
			case "unset":
				_gameState.setFlag(flagName, false);
				break;
		}
	}

	private void executeDoor(string[] tokens) {
		// Format: door.action.door_id
		if(tokens.Length < 3) {
			return;
		}

		string action = tokens[1];
		string doorId = tokens[2];

		switch(action) {
			case "unlock":
				_gameState.unlockDoor(doorId);
				break;
			case "lock":
				_gameState.lockDoor(doorId);
				break;
		}
	}

	private void executeRoom(string[] tokens) {
		// Format: room.action.room_id
		if(tokens.Length < 3) {
			return;
		}

		string action = tokens[1];
		string roomId = tokens[2];
		if(action == "travel") {
			_gameState.travelToRoom(roomId);
		}
	}

	private void executeNpc(string[] tokens) {
		// Format: npc.action.npc_id.data
		if(tokens.Length < 3) {
			return;
		}

		string action = tokens[1];
		string npcId = tokens[2];

		switch(action) {
			case "spawn":
				_gameState.spawnNPC(npcId);
				break;
			case "despawn":
				_gameState.despawnNPC(npcId);
				break;
		}
	}

	private void executeDialog(string[] tokens) {
		// Format: dialog.set_tree.npc_id.tree_id
		if(tokens.Length < 4) {
			return;
		}

		string action = tokens[1];
		string npcId = tokens[2];
		string treeId = tokens[3];

		if(action == "set_tree") {
			_gameState.setNPCDialogTree(npcId, treeId);
		}
	}
}