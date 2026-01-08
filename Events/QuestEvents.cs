using EldmeresTale.Quests;

namespace EldmeresTale.Events;

public class QuestStartedEvent : GameEvent {
	public Quest Quest { get; set; }
	public string QuestName { get; set; }
}

public class QuestCompletedEvent : GameEvent {
	public Quest Quest { get; set; }
	public QuestNode LastNode { get; set; }
	public string QuestName { get; set; }
}

public class QuestObjectiveUpdatedEvent : GameEvent {
	public Quest Quest { get; set; }
	public QuestObjective Objective { get; set; }
}

public class QuestNodeAdvancedEvent : GameEvent {
	public Quest Quest { get; set; }
	public string OldNodeId { get; set; }
	public string NewNodeId { get; set; }
}
