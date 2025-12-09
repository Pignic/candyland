using System.Collections.Generic;
using System.Linq;
using Candyland.Entities;

namespace Candyland.Dialog
{
    /// <summary>
    /// Evaluates conditions for dialog choices
    /// </summary>
    public class ConditionEvaluator
    {
        private Player _player;
        private GameStateManager _gameState;

        public ConditionEvaluator(Player player, GameStateManager gameState)
        {
            _player = player;
            _gameState = gameState;
        }

        /// <summary>
        /// Evaluate all conditions (must all be true)
        /// </summary>
        public bool EvaluateAll(List<string> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (var condition in conditions)
            {
                if (!Evaluate(condition))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Evaluate a single condition
        /// </summary>
        public bool Evaluate(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            // Handle negation
            bool negate = condition.StartsWith("!");
            if (negate)
            {
                condition = condition.Substring(1);
            }

            // Handle AND operator
            if (condition.Contains("&&"))
            {
                var parts = condition.Split(new[] { "&&" }, System.StringSplitOptions.None);
                bool result = parts.All(p => Evaluate(p.Trim()));
                return negate ? !result : result;
            }

            // Handle OR operator
            if (condition.Contains("||"))
            {
                var parts = condition.Split(new[] { "||" }, System.StringSplitOptions.None);
                bool result = parts.Any(p => Evaluate(p.Trim()));
                return negate ? !result : result;
            }

            // Parse condition type
            var tokens = condition.Split('.');
            if (tokens.Length < 2)
                return true;

            string category = tokens[0];
            bool result_eval = false;

            switch (category)
            {
                case "quest":
                    result_eval = EvaluateQuest(tokens);
                    break;

                case "item":
                    result_eval = EvaluateItem(tokens);
                    break;

                case "player":
                    result_eval = EvaluatePlayer(tokens);
                    break;

                case "flag":
                    result_eval = EvaluateFlag(tokens);
                    break;

                case "time":
                    result_eval = EvaluateTime(tokens);
                    break;

                case "room":
                    result_eval = EvaluateRoom(tokens);
                    break;

                default:
                    result_eval = true;
                    break;
            }

            return negate ? !result_eval : result_eval;
        }

        private bool EvaluateQuest(string[] tokens)
        {
            // Format: quest.quest_id.status
            // Example: quest.clear_forest.completed
            if (tokens.Length < 3)
                return false;

            string questId = tokens[1];
            string status = tokens[2];

            return _gameState.CheckQuestStatus(questId, status);
        }

        private bool EvaluateItem(string[] tokens)
        {
            // Format: item.has.item_id or item.has.item_id >= count
            if (tokens.Length < 3)
                return false;

            string operation = tokens[1]; // "has"
            string itemId = tokens[2];

            if (operation == "has")
            {
                // Check for comparison operators in the original condition
                if (itemId.Contains(">=") || itemId.Contains("<=") || itemId.Contains(">") || itemId.Contains("<") || itemId.Contains("=="))
                {
                    // Parse: item_id >= value
                    string op = "";
                    if (itemId.Contains(">=")) op = ">=";
                    else if (itemId.Contains("<=")) op = "<=";
                    else if (itemId.Contains(">")) op = ">";
                    else if (itemId.Contains("<")) op = "<";
                    else if (itemId.Contains("==")) op = "==";

                    var parts = itemId.Split(new[] { op }, System.StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string actualItemId = parts[0].Trim();
                        int requiredCount = int.Parse(parts[1].Trim());
                        int actualCount = _gameState.GetItemCount(actualItemId);

                        return op switch
                        {
                            ">=" => actualCount >= requiredCount,
                            "<=" => actualCount <= requiredCount,
                            ">" => actualCount > requiredCount,
                            "<" => actualCount < requiredCount,
                            "==" => actualCount == requiredCount,
                            _ => false
                        };
                    }
                }
                else
                {
                    // Simple has check
                    return _gameState.HasItem(itemId);
                }
            }

            return false;
        }

        private bool EvaluatePlayer(string[] tokens)
        {
            // Format: player.stat >= value
            // Example: player.level >= 5, player.health > 50
            if (tokens.Length < 2)
                return false;

            string stat = tokens[1];

            // Extract comparison operator and value
            string comparison = "";
            int value = 0;

            foreach (var op in new[] { ">=", "<=", "==", ">", "<" })
            {
                if (stat.Contains(op))
                {
                    var parts = stat.Split(new[] { op }, System.StringSplitOptions.None);
                    stat = parts[0].Trim();
                    value = int.Parse(parts[1].Trim());
                    comparison = op;
                    break;
                }
            }

            if (string.IsNullOrEmpty(comparison))
                return false;

            int actualValue = stat.ToLower() switch
            {
                "level" => _player.Level,
                "health" => _player.Health,
                "maxhealth" => _player.MaxHealth,
                "coins" => _player.Coins,
                "xp" => _player.XP,
                _ => 0
            };

            return comparison switch
            {
                ">=" => actualValue >= value,
                "<=" => actualValue <= value,
                ">" => actualValue > value,
                "<" => actualValue < value,
                "==" => actualValue == value,
                _ => false
            };
        }

        private bool EvaluateFlag(string[] tokens)
        {
            // Format: flag.flag_name
            // Example: flag.met_elder
            if (tokens.Length < 2)
                return false;

            string flagName = tokens[1];
            return _gameState.GetFlag(flagName);
        }

        private bool EvaluateTime(string[] tokens)
        {
            // Format: time.is_day or time.is_night
            if (tokens.Length < 2)
                return false;

            string timeCheck = tokens[1];
            return _gameState.CheckTime(timeCheck);
        }

        private bool EvaluateRoom(string[] tokens)
        {
            // Format: room.current.room_id
            if (tokens.Length < 3)
                return false;

            string operation = tokens[1]; // "current"
            string roomId = tokens[2];

            if (operation == "current")
            {
                return _gameState.GetCurrentRoom() == roomId;
            }

            return false;
        }
    }
}