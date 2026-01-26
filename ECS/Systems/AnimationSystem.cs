using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;

namespace EldmeresTale.ECS.Systems;

public sealed class AnimationSystem : AEntitySetSystem<float> {

	public AnimationSystem(World world)
		: base(world.GetEntities()
			.With<RoomActive>()
			.With<Animation>()
			.AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Animation animation = ref entity.Get<Animation>();
		animation.Update(deltaTime);

		// Update sprite source rect if entity has Sprite component
		if (entity.Has<Sprite>()) {
			if (entity.Has<Velocity>()) {
				Velocity velocity = entity.Get<Velocity>();
				if (velocity.Value.X < 0) {
					animation.UpdateDirection(Direction.Right);
				} else if (velocity.Value.X > 0) {
					animation.UpdateDirection(Direction.Left);
				} else if (velocity.Value.Y < 0) {
					animation.UpdateDirection(Direction.Up);
				} else if (velocity.Value.Y > 0) {
					animation.UpdateDirection(Direction.Down);
				} else {
					animation.IsPlaying = false;
				}
			}
			ref Sprite sprite = ref entity.Get<Sprite>();
			sprite.SourceRect = animation.GetSourceRect();
		}
	}
}