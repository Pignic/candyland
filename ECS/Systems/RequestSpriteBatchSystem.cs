using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components.Command;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.ECS.Systems;

public sealed class RequestSpriteBatchSystem : AEntitySetSystem<SpriteBatch> {

	public RequestSpriteBatchSystem(World world)
		: base(world.GetEntities()
			.With<RequestSpriteBatch>()
			.AsSet()) {

	}

	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {
		entity.Get<RequestSpriteBatch>().action?.Invoke(spriteBatch);
		base.Update(spriteBatch, entity);
	}

	protected override void PostUpdate(SpriteBatch spriteBatch) {
		foreach (Entity entity in Set.GetEntities()) {
			entity.Remove<RequestSpriteBatch>();
		}
		base.PostUpdate(spriteBatch);
	}
}