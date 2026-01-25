using EldmeresTale.Quests;

namespace EldmeresTale.Events;

public abstract class QuestEvent : GameEvent {
	public Quest Quest { get; set; }
	public string QuestName { get; set; }

}

public abstract class QuestAdvancedEvent : QuestEvent {
	public QuestNode LastNode { get; set; }

}

public class QuestStartedEvent : QuestEvent {
}

public class QuestCompletedEvent : QuestAdvancedEvent {
}

public class QuestObjectiveUpdatedEvent : QuestEvent {
	public QuestObjective Objective { get; set; }
}

public class QuestNodeAdvancedEvent : QuestAdvancedEvent {
	public string NewNodeId { get; set; }
}
