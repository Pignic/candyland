using System.Collections.Generic;

namespace EldmeresTale.Core.Saves;

public class QuestSaveData {
	public List<ActiveQuestData> ActiveQuests { get; set; }

	public List<string> CompletedQuests { get; set; }

	public QuestSaveData() {
		ActiveQuests = [];
		CompletedQuests = [];
	}
}

public class ActiveQuestData {

	public string QuestId { get; set; }

	public string CurrentNodeId { get; set; }

	public Dictionary<string, int> ObjectiveProgress { get; set; }

	public ActiveQuestData() {
		QuestId = "";
		CurrentNodeId = "";
		ObjectiveProgress = [];
	}
}