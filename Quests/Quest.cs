using System.Collections.Generic;

namespace Candyland.Quests;

/// <summary>
/// Quest definition - the template for a quest
/// </summary>
public class Quest {
	public string id { get; set; }
	public string nameKey { get; set; }  // Localization key
	public string descriptionKey { get; set; }  // Localization key
	public string startNodeId { get; set; }
	public string questGiver { get; set; }
	public List<string> requirements { get; set; }  // Conditions to accept quest
	public Dictionary<string, QuestNode> nodes { get; set; }

	public Quest() {
		requirements = new List<string>();
		nodes = new Dictionary<string, QuestNode>();
	}

	public QuestNode startNode() {
		return nodes[startNodeId];
	}
}

/// <summary>
/// A step/phase in a quest
/// Similar to DialogNode but with objectives instead of text
/// </summary>
public class QuestNode {
	public string id { get; set; }
	public string descriptionKey { get; set; }  // Localization key for node description
	public List<QuestObjective> objectives { get; set; }

	// What happens when all objectives complete
	public List<string> onCompleteEffects { get; set; }
	public QuestReward rewards { get; set; }
	public List<QuestBranch> branches { get; set; }  // Conditional next nodes
	public string nextNodeId { get; set; }  // Simple next node (no conditions)

	public QuestNode() {
		objectives = new List<QuestObjective>();
		onCompleteEffects = new List<string>();
		branches = new List<QuestBranch>();
	}
}

/// <summary>
/// A single objective within a quest node
/// </summary>
public class QuestObjective {
	public string type { get; set; }  // "kill_enemy", "collect_item", "talk_to_npc", etc.
	public string target { get; set; }  // enemy/item/npc id
	public int requiredCount { get; set; }  // How many needed
	public string descriptionKey { get; set; }  // Localization key

	public QuestObjective() {
		requiredCount = 1;
	}

	// Override Equals and GetHashCode so objectives can be used as dictionary keys
	public override bool Equals(object obj) {
		if(obj is QuestObjective other) {
			return type == other.type &&
				   target == other.target &&
				   descriptionKey == other.descriptionKey;
		}
		return false;
	}

	public override int GetHashCode() {
		return System.HashCode.Combine(type, target, descriptionKey);
	}
}

/// <summary>
/// Conditional branch to next quest node
/// </summary>
public class QuestBranch {
	public List<string> conditions { get; set; }
	public string nextNodeId { get; set; }

	public QuestBranch() {
		conditions = new List<string>();
	}
}

/// <summary>
/// Rewards given when quest/node completes
/// </summary>
public class QuestReward {
	public int xp { get; set; }
	public int gold { get; set; }
	public List<string> items { get; set; }

	public QuestReward() {
		items = new List<string>();
	}
}

/// <summary>
/// Active quest instance - tracks progress for a specific player
/// </summary>
public class QuestInstance {
	public Quest quest { get; set; }
	public string currentNodeId { get; set; }
	public Dictionary<QuestObjective, int> objectiveProgress { get; set; }

	public QuestInstance(Quest quest) {
		this.quest = quest;
		this.currentNodeId = quest.startNodeId;
		this.objectiveProgress = new Dictionary<QuestObjective, int>();
	}

	public QuestNode getCurrentNode() {
		if(currentNodeId != null && quest.nodes.ContainsKey(currentNodeId)) {
			return quest.nodes[currentNodeId];
		}
		return null;
	}

	public void goToNode(string nodeId) {
		if(nodeId == "end" || nodeId == null) {
			currentNodeId = null;
		} else if(quest.nodes.ContainsKey(nodeId)) {
			currentNodeId = nodeId;
			// Clear progress for new node
			objectiveProgress.Clear();
		}
	}

	public bool isNodeComplete() {
		var currentNode = getCurrentNode();
		if(currentNode == null) return true;

		foreach(var objective in currentNode.objectives) {
			if(!objectiveProgress.ContainsKey(objective))
				return false;

			if(objectiveProgress[objective] < objective.requiredCount)
				return false;
		}

		return true;
	}
}