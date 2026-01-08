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

	public void Execute(string effect) {
		if (string.IsNullOrEmpty(effect)) {
			return;
		}

		string[] tokens = effect.Split('.');
		if (tokens.Length < 2) {
			return;
		}

		switch (tokens[0]) {
			case "quest":
				ExecuteQuest(tokens);
				break;
			case "item":
				ExecuteItem(tokens);
				break;
			case "player":
				ExecutePlayer(tokens);
				break;
			case "flag":
				ExecuteFlag(tokens);
				break;
			case "door":
				ExecuteDoor(tokens);
				break;
			case "room":
				ExecuteRoom(tokens);
				break;
			case "npc":
				ExecuteNpc(tokens);
				break;
			case "dialog":
				ExecuteDialog(tokens);
				break;
			default:
				break;
		}
	}

	private void ExecuteQuest(string[] tokens) {
		// Format: quest.action.quest_id
		if (tokens.Length < 3) {
			System.Diagnostics.Debug.WriteLine("[EFFECT] Invalid quest effect format");
			return;
		}

		string action = tokens[1];
		string questId = tokens[2];

		System.Diagnostics.Debug.WriteLine($"[EFFECT] Quest effect: {action} -> {questId}");

		switch (action) {
			case "start":
				// Use QuestManager if available
				if (_questManager != null) {
					System.Diagnostics.Debug.WriteLine($"[EFFECT] Calling questManager.startQuest({questId})");
					bool success = _questManager.StartQuest(questId);
					System.Diagnostics.Debug.WriteLine($"[EFFECT] Quest start result: {success}");
				} else {
					// Fallback to old GameState method
					_gameState.StartQuest(questId);
					System.Diagnostics.Debug.WriteLine("[EFFECT] WARNING: QuestManager is null!");
				}
				break;
			case "complete":
				// Quest completion is automatic in QuestManager when objectives done
				// But keep this for manual completion or old GameState
				_gameState.CompleteQuest(questId);
				break;
			case "fail":
				_gameState.FailQuest(questId);
				break;
		}
	}

	private void ExecuteItem(string[] tokens) {
		// Format: item.action.item_id.count
		// Example: item.give.health_potion.3, item.remove.quest_item
		if (tokens.Length < 3) {
			return;
		}
		string action = tokens[1];
		string itemId = tokens[2];
		int count = tokens.Length >= 4 ? int.Parse(tokens[3]) : 1;

		switch (action) {
			case "give":
				_gameState.GiveItem(itemId, count);
				break;
			case "remove":
				_gameState.RemoveItem(itemId, count);
				break;
		}
	}

	private void ExecutePlayer(string[] tokens) {
		// Format: player.action.value
		if (tokens.Length < 3) {
			return;
		}
		string action = tokens[1];
		int value = int.Parse(tokens[2]);

		switch (action) {
			case "heal":
				_player.Health = Math.Min(_player.Health + value, _player.MaxHealth);
				break;
			case "damage":
				_player.Health = Math.Max(_player.Health - value, 0);
				break;
			case "xp":
				_player.GainXP(value);
				break;
		}
	}

	private void ExecuteFlag(string[] tokens) {
		// Format: flag.action.flag_name
		if (tokens.Length < 3) {
			return;
		}
		string action = tokens[1];
		string flagName = tokens[2];

		switch (action) {
			case "set":
				_gameState.SetFlag(flagName, true);
				break;
			case "unset":
				_gameState.SetFlag(flagName, false);
				break;
		}
	}

	private void ExecuteDoor(string[] tokens) {
		// Format: door.action.door_id
		if (tokens.Length < 3) {
			return;
		}
		string action = tokens[1];
		string doorId = tokens[2];

		switch (action) {
			case "unlock":
				_gameState.UnlockDoor(doorId);
				break;
			case "lock":
				_gameState.LockDoor(doorId);
				break;
		}
	}

	private void ExecuteRoom(string[] tokens) {
		// Format: room.action.room_id
		if (tokens.Length < 3) {
			return;
		}
		string action = tokens[1];
		string roomId = tokens[2];
		if (action == "travel") {
			_gameState.TravelToRoom(roomId);
		}
	}

	private void ExecuteNpc(string[] tokens) {
		// Format: npc.action.npc_id.data
		if (tokens.Length < 3) {
			return;
		}
		string action = tokens[1];
		string npcId = tokens[2];

		switch (action) {
			case "spawn":
				_gameState.SpawnNPC(npcId);
				break;
			case "despawn":
				_gameState.DespawnNPC(npcId);
				break;
		}
	}

	private void ExecuteDialog(string[] tokens) {
		// Format: dialog.set_tree.npc_id.tree_id
		if (tokens.Length < 4) {
			return;
		}
		string action = tokens[1];
		string npcId = tokens[2];
		string treeId = tokens[3];

		if (action == "set_tree") {
			_gameState.SetNPCDialogTree(npcId, treeId);
		}
	}
}