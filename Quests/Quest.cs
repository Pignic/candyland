using System.Collections.Generic;

namespace EldmeresTale.Quests;

public class Quest {
	public string Id { get; set; }
	public string NameKey { get; set; }  // Localization key
	public string DescriptionKey { get; set; }  // Localization key
	public string StartNodeId { get; set; }
	public string QuestGiver { get; set; }
	public List<string> Requirements { get; set; }  // Conditions to accept quest
	public Dictionary<string, QuestNode> Nodes { get; set; }

	public Quest() {
		Requirements = [];
		Nodes = [];
	}

	public QuestNode StartNode() {
		return Nodes[StartNodeId];
	}
}

public class QuestNode {
	public string Id { get; set; }
	public string DescriptionKey { get; set; }  // Localization key for node description
	public List<QuestObjective> Objectives { get; set; }

	// What happens when all objectives complete
	public List<string> OnCompleteEffects { get; set; }
	public QuestReward Rewards { get; set; }
	public List<QuestBranch> Branches { get; set; }  // Conditional next nodes
	public string NextNodeId { get; set; }  // Simple next node (no conditions)

	public QuestNode() {
		Objectives = [];
		OnCompleteEffects = [];
		Branches = [];
	}
}

public class QuestObjective {
	public string Type { get; set; }  // "kill_enemy", "collect_item", "talk_to_npc", etc.
	public string Target { get; set; }  // enemy/item/npc id
	public int RequiredCount { get; set; }  // How many needed
	public string DescriptionKey { get; set; }  // Localization key

	public QuestObjective() {
		RequiredCount = 1;
	}

	public override bool Equals(object obj) {
		if (obj is QuestObjective other) {
			return Type == other.Type &&
				   Target == other.Target &&
				   DescriptionKey == other.DescriptionKey;
		}
		return false;
	}

	public override int GetHashCode() {
		return System.HashCode.Combine(Type, Target, DescriptionKey);
	}
}

public class QuestBranch {
	public List<string> Conditions { get; set; }
	public string NextNodeId { get; set; }

	public QuestBranch() {
		Conditions = [];
	}
}

public class QuestReward {
	public int Xp { get; set; }
	public int Gold { get; set; }
	public List<string> Items { get; set; }

	public QuestReward() {
		Items = [];
	}
}

public class QuestInstance {
	public Quest Quest { get; set; }
	public string CurrentNodeId { get; set; }
	public Dictionary<QuestObjective, int> ObjectiveProgress { get; set; }

	public QuestInstance(Quest quest) {
		Quest = quest;
		CurrentNodeId = quest.StartNodeId;
		ObjectiveProgress = [];
	}

	public QuestNode GetCurrentNode() {
		if (CurrentNodeId != null && Quest.Nodes.TryGetValue(CurrentNodeId, out QuestNode value)) {
			return value;
		}
		return null;
	}

	public void GoToNode(string nodeId) {
		if (nodeId == "end" || nodeId == null) {
			CurrentNodeId = null;
		} else if (Quest.Nodes.ContainsKey(nodeId)) {
			CurrentNodeId = nodeId;
			// Clear progress for new node
			ObjectiveProgress.Clear();
		}
	}

	public bool IsNodeComplete() {
		QuestNode currentNode = GetCurrentNode();
		if (currentNode == null) {
			return true;
		}

		foreach (QuestObjective objective in currentNode.Objectives) {
			if (!ObjectiveProgress.TryGetValue(objective, out int value)) {
				return false;
			}
			if (value < objective.RequiredCount) {
				return false;
			}
		}
		return true;
	}
}