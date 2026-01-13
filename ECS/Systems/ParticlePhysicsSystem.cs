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

		// Fade alpha
		float alphaReduction = particle.FadeSpeed * deltaTime;
		float currentAlpha = particle.Color.A / 255f;
		currentAlpha -= alphaReduction;

		if (currentAlpha <= 0) {
			// Fully faded - destroy particle
			lifetime.Remaining = 0;
			return;
		}

		particle.Color *= currentAlpha;

		// Fade size if enabled
		if (particle.FadeSize) {
			float lifetimeRatio = lifetime.Remaining / lifetime.Duartion;
			particle.Size = particle.InitialSize * lifetimeRatio;

			if (particle.Size < 0.5f) {
				lifetime.Remaining = 0;
			}
		}
	}
}