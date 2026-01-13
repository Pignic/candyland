using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EldmeresTale.ECS.Systems;

public sealed class DeathAnimationSystem : AEntitySetSystem<float> {
	private readonly List<Entity> _entitiesToDispose = new(64);

	public DeathAnimationSystem(World world)
		: base(world.GetEntities()
			.With<DeathAnimation>()
			.With<Sprite>()
			.AsSet()) {
	}

	protected override void PreUpdate(float state) {
		_entitiesToDispose.Clear();
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref DeathAnimation death = ref entity.Get<DeathAnimation>();
		ref Sprite sprite = ref entity.Get<Sprite>();

		death.Timer += deltaTime;

		// Update rotation
		death.CurrentRotation += death.RotationSpeed * deltaTime;
		sprite.Rotation = death.CurrentRotation;

		// Update scale (shrink)
		death.CurrentScale = Math.Max(0, 1f - (death.Timer / death.Duration));
		sprite.Scale = new Vector2(death.CurrentScale, death.CurrentScale);

		// Update alpha (fade)
		float alpha = 1f - death.Progress;
		sprite.Tint = death.InitialColor * alpha;

		// Check if complete
		if (death.IsComplete) {
			_entitiesToDispose.Add(entity);
		}
	}

	protected override void PostUpdate(float state) {
		foreach (Entity entity in _entitiesToDispose) {
			entity.Dispose();
		}
	}
}