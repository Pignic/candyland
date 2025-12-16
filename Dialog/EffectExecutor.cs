using Candyland.Entities;
using System;

namespace Candyland.Dialog;

public class EffectExecutor {
	private Player _player;
	private GameStateManager _gameState;

	public EffectExecutor(Player player, GameStateManager gameState) {
		_player = player;
		_gameState = gameState;
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
			return;
		}

		string action = tokens[1];
		string questId = tokens[2];

		switch(action) {
			case "start":
				_gameState.startQuest(questId);
				break;
			case "complete":
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
		if(tokens.Length < 3){
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
		if(tokens.Length < 3){
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
		if(tokens.Length < 3){
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
		if(tokens.Length < 3){
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
		if(tokens.Length < 3){
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
		if(tokens.Length < 3){
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
		if(tokens.Length < 4){
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