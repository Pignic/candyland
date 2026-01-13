using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class ParticleRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Camera _camera;
	private static Texture2D _pixelTexture;

	public ParticleRenderSystem(World world, Camera camera, GraphicsDevice graphicsDevice)
		: base(world.GetEntities()
			.With<Position>()
			.With<ParticleData>()
			.AsSet()) {
		_camera = camera;

		// Create white pixel texture if not exists
		if (_pixelTexture == null) {
			_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			_pixelTexture.SetData([Color.White]);
		}
	}

	protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> entities) {
		foreach (Entity entity in entities) {
			DrawParticle(spriteBatch, entity);
		}
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
			_pixelTexture,
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