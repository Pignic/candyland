using System.Collections.Generic;

namespace EldmeresTale.Core.Saves;

public class QuestSaveData {
	/// <summary>
	/// Currently active quests with their progress
	/// </summary>
	public List<ActiveQuestData> ActiveQuests { get; set; }

	/// <summary>
	/// IDs of completed quests
	/// </summary>
	public List<string> CompletedQuests { get; set; }

	public QuestSaveData() {
		ActiveQuests = new List<ActiveQuestData>();
		CompletedQuests = new List<string>();
	}
}

/// <summary>
/// Saved state for a single active quest
/// </summary>
public class ActiveQuestData {
	/// <summary>
	/// Quest ID (e.g., "find_sword_quest")
	/// </summary>
	public string QuestId { get; set; }

	/// <summary>
	/// Current node in the quest progression
	/// </summary>
	public string CurrentNodeId { get; set; }

	/// <summary>
	/// Progress on each objective
	/// Key: Objective description (e.g., "kill_goblin")
	/// Value: Current progress count
	/// </summary>
	public Dictionary<string, int> ObjectiveProgress { get; set; }

	public ActiveQuestData() {
		QuestId = "";
		CurrentNodeId = "";
		ObjectiveProgress = new Dictionary<string, int>();
	}
}