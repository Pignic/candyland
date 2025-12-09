using System.Collections.Generic;

namespace Candyland.Dialog
{
    /// <summary>
    /// Manages game state for conditions and effects
    /// </summary>
    public class GameStateManager
    {
        // Quest tracking
        private Dictionary<string, QuestStatus> _quests;

        // Item inventory (simplified - could use player's actual inventory)
        private Dictionary<string, int> _items;

        // Game flags
        private Dictionary<string, bool> _flags;

        // Current room
        private string _currentRoom;

        // NPC dialog tree overrides
        private Dictionary<string, string> _npcDialogTrees;

        // Time state
        private bool _isDay = true;

        public GameStateManager()
        {
            _quests = new Dictionary<string, QuestStatus>();
            _items = new Dictionary<string, int>();
            _flags = new Dictionary<string, bool>();
            _npcDialogTrees = new Dictionary<string, string>();
            _currentRoom = "";
        }

        #region Quest Management

        public void StartQuest(string questId)
        {
            _quests[questId] = QuestStatus.Active;
            System.Diagnostics.Debug.WriteLine($"Quest started: {questId}");
        }

        public void CompleteQuest(string questId)
        {
            _quests[questId] = QuestStatus.Completed;
            System.Diagnostics.Debug.WriteLine($"Quest completed: {questId}");
        }

        public void FailQuest(string questId)
        {
            _quests[questId] = QuestStatus.Failed;
            System.Diagnostics.Debug.WriteLine($"Quest failed: {questId}");
        }

        public bool CheckQuestStatus(string questId, string status)
        {
            if (!_quests.ContainsKey(questId))
            {
                // Quest not started
                return status == "not_started" || status == "!started";
            }

            QuestStatus questStatus = _quests[questId];

            return status.ToLower() switch
            {
                "active" => questStatus == QuestStatus.Active,
                "completed" => questStatus == QuestStatus.Completed,
                "failed" => questStatus == QuestStatus.Failed,
                "started" => questStatus != QuestStatus.NotStarted,
                "not_started" => questStatus == QuestStatus.NotStarted,
                _ => false
            };
        }

        #endregion

        #region Item Management

        public void GiveItem(string itemId, int count)
        {
            if (!_items.ContainsKey(itemId))
                _items[itemId] = 0;

            _items[itemId] += count;
            System.Diagnostics.Debug.WriteLine($"Item given: {itemId} x{count}");
        }

        public void RemoveItem(string itemId, int count)
        {
            if (_items.ContainsKey(itemId))
            {
                _items[itemId] -= count;
                if (_items[itemId] <= 0)
                    _items.Remove(itemId);

                System.Diagnostics.Debug.WriteLine($"Item removed: {itemId} x{count}");
            }
        }

        public bool HasItem(string itemId)
        {
            return _items.ContainsKey(itemId) && _items[itemId] > 0;
        }

        public int GetItemCount(string itemId)
        {
            return _items.ContainsKey(itemId) ? _items[itemId] : 0;
        }

        #endregion

        #region Flag Management

        public void SetFlag(string flagName, bool value)
        {
            _flags[flagName] = value;
            System.Diagnostics.Debug.WriteLine($"Flag set: {flagName} = {value}");
        }

        public bool GetFlag(string flagName)
        {
            return _flags.ContainsKey(flagName) && _flags[flagName];
        }

        #endregion

        #region Room Management

        public void SetCurrentRoom(string roomId)
        {
            _currentRoom = roomId;
        }

        public string GetCurrentRoom()
        {
            return _currentRoom;
        }

        public void TravelToRoom(string roomId)
        {
            _currentRoom = roomId;
            System.Diagnostics.Debug.WriteLine($"Traveled to room: {roomId}");
            // In actual implementation, this would trigger room transition in game
        }

        #endregion

        #region Time Management

        public void SetDayNight(bool isDay)
        {
            _isDay = isDay;
        }

        public bool CheckTime(string timeCheck)
        {
            return timeCheck.ToLower() switch
            {
                "is_day" => _isDay,
                "is_night" => !_isDay,
                _ => true
            };
        }

        #endregion

        #region Door Management

        public void UnlockDoor(string doorId)
        {
            SetFlag($"door_{doorId}_unlocked", true);
            System.Diagnostics.Debug.WriteLine($"Door unlocked: {doorId}");
        }

        public void LockDoor(string doorId)
        {
            SetFlag($"door_{doorId}_unlocked", false);
            System.Diagnostics.Debug.WriteLine($"Door locked: {doorId}");
        }

        #endregion

        #region NPC Management

        public void SpawnNPC(string npcId)
        {
            SetFlag($"npc_{npcId}_spawned", true);
            System.Diagnostics.Debug.WriteLine($"NPC spawned: {npcId}");
            // In actual implementation, this would add NPC to current room
        }

        public void DespawnNPC(string npcId)
        {
            SetFlag($"npc_{npcId}_spawned", false);
            System.Diagnostics.Debug.WriteLine($"NPC despawned: {npcId}");
            // In actual implementation, this would remove NPC from current room
        }

        public void SetNPCDialogTree(string npcId, string treeId)
        {
            _npcDialogTrees[npcId] = treeId;
            System.Diagnostics.Debug.WriteLine($"NPC dialog changed: {npcId} -> {treeId}");
        }

        public string GetNPCDialogTree(string npcId)
        {
            return _npcDialogTrees.ContainsKey(npcId) ? _npcDialogTrees[npcId] : null;
        }

        #endregion
    }

    /// <summary>
    /// Quest status enum
    /// </summary>
    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed
    }
}