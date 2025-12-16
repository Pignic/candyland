using System.Collections.Generic;
using System.Linq;
using Candyland.Entities;

namespace Candyland.Dialog;

public class ConditionEvaluator {

	private readonly GameStateManager gameState;
	private readonly Player player;

	public ConditionEvaluator(Player player, GameStateManager gameState) {
		this.player = player;
		this.gameState = gameState;
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
		return gameState.checkQuestStatus(tokens[1], tokens[2]);
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
					int actualCount = gameState.getItemCount(actualItemId);

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
				return gameState.hasItem(itemId);
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
			"health" => player.health,
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
		return gameState.getFlag(tokens[1]);
	}

	private bool EvaluateRoom(string[] tokens) {
		// Format: room.current.room_id
		if(tokens.Length < 3) {
			return false;
		}
		if(tokens[1] == "current") {
			return gameState.getCurrentRoom() == tokens[2];
		}

		return false;
	}

	private bool EvaluateTime(string[] tokens) {
		// Format: time.is_day or time.is_night
		if(tokens.Length < 2) {
			return false;
		}
		return gameState.checkTime(tokens[1]);
	}
}