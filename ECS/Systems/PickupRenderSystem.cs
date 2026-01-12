using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace EldmeresTale.ECS.Systems;

public class PickupRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Camera _camera;

	public PickupRenderSystem(DefaultEcs.World world, Camera camera)
		: base(world.GetEntities()
			.With<Position>()
			.With<Sprite>()
			.AsSet()) {
		_camera = camera;
	}

	protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> entities) {
		// Sort by Y for depth (painter's algorithm)
		IOrderedEnumerable<Entity> sorted = entities.ToArray()
			.OrderBy(e => e.Get<Position>().Value.Y);

		foreach (Entity entity in sorted) {
			DrawEntity(spriteBatch, entity);
		}
	}

	private void DrawEntity(SpriteBatch spriteBatch, Entity entity) {
		Position pos = entity.Get<Position>();
		Sprite sprite = entity.Get<Sprite>();

		spriteBatch.Draw(
			sprite.Texture,
			pos.Value,
			sprite.SourceRect,
			sprite.Tint,
			sprite.Rotation,
			sprite.Origin,
			sprite.Scale,
			SpriteEffects.None,
			0f
		);
	}
}