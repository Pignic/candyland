using EldmeresTale.Events;

namespace EldmeresTale.ECS.Components;

public struct ECSEvent {

	public GameEvent Event;

	public ECSEvent(GameEvent gameEvent) {
		Event = gameEvent;
	}
}
