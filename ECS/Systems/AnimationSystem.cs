using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;

namespace EldmeresTale.ECS.Systems;

public sealed class AnimationSystem : AEntitySetSystem<float> {

	public AnimationSystem(DefaultEcs.World world)
		: base(world.GetEntities()
			.With<Animation>()
			.AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Animation animation = ref entity.Get<Animation>();
		animation.Update(deltaTime);

		// Update sprite source rect if entity has Sprite component
		if (entity.Has<Sprite>()) {
			ref Sprite sprite = ref entity.Get<Sprite>();
			sprite.SourceRect = animation.GetSourceRect();
		}
	}
}