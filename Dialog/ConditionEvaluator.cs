using System.Collections.Generic;
using System.Linq;
using EldmeresTale.Entities;
using EldmeresTale.Quests;

namespace EldmeresTale.Dialog;

public class ConditionEvaluator {

	private readonly Player player;
	private readonly GameStateManager gameState;
	private QuestManager _questManager;  // Set after initialization

	public ConditionEvaluator(Player player, GameStateManager gameState) {
		this.player = player;
		this.gameState = gameState;
	}

	public void SetQuestManager(QuestManager questManager) {
		_questManager = questManager;
	}

	public bool EvaluateAll(List<string> conditions) {
		if(conditions == null || conditions.Count == 0) {
			return true;
		}

		foreach(var condition in conditions) {
			if(!Evaluate(condition)) {
				return false;
			}
		}
		return true;
	}

	public bool Evaluate(string condition) {
		if(string.IsNullOrEmpty(condition)) {
			return true;
		}

		// Handle negation
		bool negate = condition.StartsWith("!");
		if(negate) {
			condition = condition.Substring(1);
		}

		// Handle AND operator
		if(condition.Contains("&&")) {
			var parts = condition.Split("&&");
			bool result = parts.All(p => Evaluate(p.Trim()));
			return negate ? !result : result;
		}

		// Handle OR operator
		if(condition.Contains("||")) {
			var parts = condition.Split("||");
			bool result = parts.Any(p => Evaluate(p.Trim()));
			return negate ? !result : result;
		}

		// Parse condition type
		var tokens = condition.Split('.');
		if(tokens.Length < 2) {
			return true;
		}

		string category = tokens[0];
		bool result_eval = false;

		result_eval = category switch {
			"quest" => EvaluateQuest(tokens),
			"item" => EvaluateItem(tokens),
			"player" => EvaluatePlayer(tokens),
			"flag" => EvaluateFlag(tokens),
			"time" => EvaluateTime(tokens),
			"room" => EvaluateRoom(tokens),
			_ => true,
		};
		return negate ? !result_eval : result_eval;
	}

	private bool EvaluateQuest(string[] tokens) {
		// Format: quest.quest_id.status
		
		if(tokens.Length < 3) {
			return false;
		}

		string questId = tokens[1];
		string status = tokens[2];

		if(status.StartsWith("node:")) {
			string nodeId = status.Substring(5);  // Remove "node:"
			if(_questManager != null) {
				return _questManager.IsQuestOnNode(questId, nodeId);
			}
			return false;
		}

		// Use QuestManager if available (preferred)
		if(_questManager != null) {
			switch(status.ToLower()) {
				case "active":
					return _questManager.IsQuestActive(questId);
				case "completed":
					return _questManager.IsQuestCompleted(questId);
				case "not_started":
					return !_questManager.IsQuestActive(questId) &&
						   !_questManager.IsQuestCompleted(questId);
				case "can_accept":
					return _questManager.CanAcceptQuest(questId);
				default:
					return false;
			}
		}

		// Fallback to old GameStateManager method
		return gameState.CheckQuestStatus(questId, status);
	}

	private bool EvaluateItem(string[] tokens) {
		// Format: item.has.item_id or item.has.item_id >= count
		if(tokens.Length < 3) {
			return false;
		}

		string operation = tokens[1]; // "has"
		string itemId = tokens[2];

		if(operation == "has") {
			// Check for comparison operators in the original condition
			if(itemId.Contains(">=") || itemId.Contains("<=") || itemId.Contains(">") || itemId.Contains("<") || itemId.Contains("==")) {
				// Parse: item_id >= value
				string op = "";
				if(itemId.Contains(">=")) {
					op = ">=";
				} else if(itemId.Contains("<=")) {
					op = "<=";
				} else if(itemId.Contains(">")) {
					op = ">";
				} else if(itemId.Contains("<")) {
					op = "<";
				} else if(itemId.Contains("==")) {
					op = "==";
				}

				var parts = itemId.Split(op);
				if(parts.Length == 2) {
					string actualItemId = parts[0].Trim();
					int requiredCount = int.Parse(parts[1].Trim());
					int actualCount = gameState.GetItemCount(actualItemId);

					return op switch {
						">=" => actualCount >= requiredCount,
						"<=" => actualCount <= requiredCount,
						">" => actualCount > requiredCount,
						"<" => actualCount < requiredCount,
						"==" => actualCount == requiredCount,
						_ => false
					};
				}
			} else {
				// Simple has check
				return gameState.HasItem(itemId);
			}
		}
		return false;
	}

	private bool EvaluatePlayer(string[] tokens) {
		// Format: player.stat >= value
		if(tokens.Length < 2) {
			return false;
		}

		string stat = tokens[1];

		// Extract comparison operator and value
		string comparison = "";
		int value = 0;

		foreach(var op in new[] { ">=", "<=", "==", ">", "<" }) {
			if(stat.Contains(op)) {
				var parts = stat.Split(op);
				stat = parts[0].Trim();
				value = int.Parse(parts[1].Trim());
				comparison = op;
				break;
			}
		}

		if(string.IsNullOrEmpty(comparison)) {
			return false;
		}

		int actualValue = stat.ToLower() switch {
			"level" => player.Level,
			"health" => player.Health,
			"maxhealth" => player.MaxHealth,
			"coins" => player.Coins,
			"xp" => player.XP,
			_ => 0
		};

		return comparison switch {
			">=" => actualValue >= value,
			"<=" => actualValue <= value,
			">" => actualValue > value,
			"<" => actualValue < value,
			"==" => actualValue == value,
			_ => false
		};
	}

	private bool EvaluateFlag(string[] tokens) {
		// Format: flag.flag_name
		if(tokens.Length < 2) {
			return false;
		}
		return gameState.GetFlag(tokens[1]);
	}

	private bool EvaluateRoom(string[] tokens) {
		// Format: room.current.room_id
		if(tokens.Length < 3) {
			return false;
		}
		if(tokens[1] == "current") {
			return gameState.GetCurrentRoom() == tokens[2];
		}

		return false;
	}

	private bool EvaluateTime(string[] tokens) {
		// Format: time.is_day or time.is_night
		if(tokens.Length < 2) {
			return false;
		}
		return gameState.CheckTime(tokens[1]);
	}
}