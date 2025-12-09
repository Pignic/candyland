using Candyland.Entities;
using System;

namespace Candyland.Dialog
{
    /// <summary>
    /// Executes effects triggered by dialog choices
    /// </summary>
    public class EffectExecutor
    {
        private Player _player;
        private GameStateManager _gameState;

        public EffectExecutor(Player player, GameStateManager gameState)
        {
            _player = player;
            _gameState = gameState;
        }

        /// <summary>
        /// Execute a single effect
        /// </summary>
        public void Execute(string effect)
        {
            if (string.IsNullOrEmpty(effect))
                return;

            var tokens = effect.Split('.');
            if (tokens.Length < 2)
                return;

            string category = tokens[0];

            switch (category)
            {
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
            }
        }

        private void ExecuteQuest(string[] tokens)
        {
            // Format: quest.action.quest_id
            // Example: quest.start.clear_forest, quest.complete.clear_forest
            if (tokens.Length < 3)
                return;

            string action = tokens[1];
            string questId = tokens[2];

            switch (action)
            {
                case "start":
                    _gameState.StartQuest(questId);
                    break;
                case "complete":
                    _gameState.CompleteQuest(questId);
                    break;
                case "fail":
                    _gameState.FailQuest(questId);
                    break;
            }
        }

        private void ExecuteItem(string[] tokens)
        {
            // Format: item.action.item_id.count
            // Example: item.give.health_potion.3, item.remove.quest_item
            if (tokens.Length < 3)
                return;

            string action = tokens[1];
            string itemId = tokens[2];
            int count = tokens.Length >= 4 ? int.Parse(tokens[3]) : 1;

            switch (action)
            {
                case "give":
                    _gameState.GiveItem(itemId, count);
                    break;
                case "remove":
                    _gameState.RemoveItem(itemId, count);
                    break;
            }
        }

        private void ExecutePlayer(string[] tokens)
        {
            // Format: player.action.value
            // Example: player.heal.50, player.damage.10, player.xp.100
            if (tokens.Length < 3)
                return;

            string action = tokens[1];
            int value = int.Parse(tokens[2]);

            switch (action)
            {
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

        private void ExecuteFlag(string[] tokens)
        {
            // Format: flag.action.flag_name
            // Example: flag.set.met_elder, flag.unset.door_locked
            if (tokens.Length < 3)
                return;

            string action = tokens[1];
            string flagName = tokens[2];

            switch (action)
            {
                case "set":
                    _gameState.SetFlag(flagName, true);
                    break;
                case "unset":
                    _gameState.SetFlag(flagName, false);
                    break;
            }
        }

        private void ExecuteDoor(string[] tokens)
        {
            // Format: door.action.door_id
            // Example: door.unlock.castle_gate, door.lock.prison_cell
            if (tokens.Length < 3)
                return;

            string action = tokens[1];
            string doorId = tokens[2];

            switch (action)
            {
                case "unlock":
                    _gameState.UnlockDoor(doorId);
                    break;
                case "lock":
                    _gameState.LockDoor(doorId);
                    break;
            }
        }

        private void ExecuteRoom(string[] tokens)
        {
            // Format: room.action.room_id
            // Example: room.travel.secret_area
            if (tokens.Length < 3)
                return;

            string action = tokens[1];
            string roomId = tokens[2];

            if (action == "travel")
            {
                _gameState.TravelToRoom(roomId);
            }
        }

        private void ExecuteNpc(string[] tokens)
        {
            // Format: npc.action.npc_id.data
            // Example: npc.spawn.mysterious_stranger
            if (tokens.Length < 3)
                return;

            string action = tokens[1];
            string npcId = tokens[2];

            switch (action)
            {
                case "spawn":
                    _gameState.SpawnNPC(npcId);
                    break;
                case "despawn":
                    _gameState.DespawnNPC(npcId);
                    break;
            }
        }

        private void ExecuteDialog(string[] tokens)
        {
            // Format: dialog.set_tree.npc_id.tree_id
            // Example: dialog.set_tree.quest_giver.quest_completed
            if (tokens.Length < 4)
                return;

            string action = tokens[1];
            string npcId = tokens[2];
            string treeId = tokens[3];

            if (action == "set_tree")
            {
                _gameState.SetNPCDialogTree(npcId, treeId);
            }
        }
    }
}