using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;

namespace EldmeresTale.ECS.Systems;

public sealed class ParticlePhysicsSystem : AEntitySetSystem<float> {

	public ParticlePhysicsSystem(World world)
		: base(world.GetEntities()
			.With<Position>()
			.With<Velocity>()
			.With<ParticleData>()
			.With<Lifetime>()
			.AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Position pos = ref entity.Get<Position>();
		ref Velocity vel = ref entity.Get<Velocity>();
		ref ParticleData particle = ref entity.Get<ParticleData>();
		ref Lifetime lifetime = ref entity.Get<Lifetime>();

		// Apply velocity
		pos.Value += vel.Value * deltaTime;

		// Apply gravity if entity has it
		if (entity.Has<Gravity>()) {
			Gravity gravity = entity.Get<Gravity>();
			vel.Value.Y += gravity.Value * deltaTime;
		}
		float lifetimeRatio = lifetime.Remaining / lifetime.Duration;
		particle.Color = particle.OriginalColor * lifetimeRatio;

		// Fade size if enabled
		if (particle.FadeSize) {
			particle.Size = particle.InitialSize * lifetimeRatio;

			if (particle.Size < 0.5f) {
				lifetime.Remaining = 0;
			}
		}
	}
}