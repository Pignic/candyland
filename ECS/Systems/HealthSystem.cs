using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;

namespace EldmeresTale.ECS.Systems;

public sealed class HealthSystem : AEntitySetSystem<float> {

	public HealthSystem(World world)
		: base(world.GetEntities()
			.With<Health>()
			.AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Health health = ref entity.Get<Health>();

		// Update invincibility timer
		if (health.InvincibilityTimer > 0) {
			health.InvincibilityTimer -= deltaTime;
			if (health.InvincibilityTimer < 0) {
				health.InvincibilityTimer = 0;
			}
		}
	}
}