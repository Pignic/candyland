using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.ECS.Systems;

public sealed class ParticleRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Camera _camera;
	private readonly Texture2D _defaultTexture;

	public ParticleRenderSystem(World world, Camera camera, Texture2D defaultTexture)
		: base(world.GetEntities()
			.With<Position>()
			.With<ParticleData>()
			.AsSet()) {
		_camera = camera;
		_defaultTexture = defaultTexture;
	}

	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {
		DrawParticle(spriteBatch, entity);
	}

	private void DrawParticle(SpriteBatch spriteBatch, Entity entity) {
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