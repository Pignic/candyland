using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using System;

namespace EldmeresTale.ECS.Systems;

public class BobAnimationSystem : AEntitySetSystem<float> {

	public BobAnimationSystem(World world)
		: base(world.GetEntities()
			.With<Position>()
			.With<BobAnimation>()
			.AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Position pos = ref entity.Get<Position>();
		ref BobAnimation bob = ref entity.Get<BobAnimation>();

		bob.Timer += deltaTime;

		// Calculate bob offset
		float bobOffset = MathF.Sin(bob.Timer * bob.Frequency) * bob.Amplitude;

		// Apply to Y position
		pos.Value.Y = bob.BaseY + bobOffset;
	}
}