using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;

namespace EldmeresTale.ECS.Systems;

public class LifetimeSystem : AEntitySetSystem<float> {

	public LifetimeSystem(World world)
		: base(world.GetEntities()
			.With<Lifetime>()
			.AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Lifetime lifetime = ref entity.Get<Lifetime>();
		lifetime.Remaining -= deltaTime;

		if (lifetime.Remaining <= 0) {
			entity.Set<ToDispose>();
		}
	}
}