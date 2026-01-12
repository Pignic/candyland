using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;

namespace EldmeresTale.ECS.Systems;

public class LifetimeSystem : AEntitySetSystem<float> {

	public LifetimeSystem(DefaultEcs.World world)
		: base(world.GetEntities()
			.With<Lifetime>()
			.AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Lifetime lifetime = ref entity.Get<Lifetime>();
		lifetime.Remaining -= deltaTime;

		if (lifetime.Remaining <= 0) {
			entity.Dispose();
		}
	}
}