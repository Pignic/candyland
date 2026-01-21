using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.Events;
using System.Reflection;

namespace EldmeresTale.ECS.Systems;

public sealed class EventSystem : AEntitySetSystem<float> {

	private readonly GameEventBus _eventBus;

	public EventSystem(World world, GameEventBus eventBus) : base(world.GetEntities().With<ECSEvent>().AsSet()) {
		_eventBus = eventBus;
	}

	protected override void Update(float state, in Entity entity) {
		PublishEvent(entity.Get<ECSEvent>().Event);
		base.Update(state, entity);
	}


	protected override void PostUpdate(float state) {
		foreach (Entity entity in Set.GetEntities()) {
			entity.Remove<ECSEvent>();
		}
	}

	private void PublishEvent(GameEvent gameEvent) {
		MethodInfo publishMethod = _eventBus.GetType()
			.GetMethod("Publish")
			.MakeGenericMethod(gameEvent.GetType());

		publishMethod.Invoke(_eventBus, [gameEvent]);

		System.Diagnostics.Debug.WriteLine($"[ECS→EVENT] Published {gameEvent.GetType().Name}");
	}

}
