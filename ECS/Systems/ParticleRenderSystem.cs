using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.ECS.Systems;

public sealed class ParticleRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Texture2D _defaultTexture;

	public ParticleRenderSystem(World world, Texture2D defaultTexture)
		: base(world.GetEntities()
			.With<RoomActive>()
			.With<Position>()
			.With<ParticleData>()
			.AsSet()) {
		_defaultTexture = defaultTexture;
	}

	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {
		Position pos = entity.Get<Position>();
		ParticleData particle = entity.Get<ParticleData>();

		// Draw as scaled circle (using white pixel)
		float diameter = particle.Size * 2f;
		Rectangle destRect = new Rectangle(
			(int)(pos.Value.X - particle.Size),
			(int)(pos.Value.Y - particle.Size),
			(int)diameter,
			(int)diameter
		);

		spriteBatch.Draw(
			_defaultTexture,
			destRect,
			null,
			particle.Color,
			0f,
			Vector2.Zero,
			SpriteEffects.None,
			0f
		);
	}
}