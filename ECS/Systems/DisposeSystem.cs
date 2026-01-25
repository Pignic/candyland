using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using System;

namespace EldmeresTale.ECS.Systems;

internal class DisposeSystem : AEntitySetSystem<float> {

	public DisposeSystem(World world)
		: base(world.GetEntities()
			.With<ToDispose>()
			.AsSet()) {

	}

	protected override void Update(float state, ReadOnlySpan<Entity> entities) {
		foreach (Entity entity in entities) {
			if (entity.Has<Sprite>()) {
				Sprite sprite = entity.Get<Sprite>();
				if (!sprite.InCache) {
					sprite.Texture.Dispose();
				}
			}
			entity.Dispose();
		}
		base.Update(state, entities);
	}
}
