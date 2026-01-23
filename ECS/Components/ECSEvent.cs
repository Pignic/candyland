using EldmeresTale.Events;

namespace EldmeresTale.ECS.Components;

public struct ECSEvent {

	public GameEvent Event;
	public bool ConsumeEntity;

	public ECSEvent(GameEvent gameEvent) {
		Event = gameEvent;
		ConsumeEntity = false;
	}

	public ECSEvent(GameEvent gameEvent, bool consumeEntity) : this(gameEvent) {
		ConsumeEntity = consumeEntity;
	}
}
