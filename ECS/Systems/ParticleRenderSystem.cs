using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.ECS.Systems;

public sealed class ParticleRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Texture2D _defaultTexture;

	readonly RoomManager _roomManager;

	public ParticleRenderSystem(World world, Texture2D defaultTexture, RoomManager roomManager)
		: base(world.GetEntities()
			.With<Position>()
			.With<RoomId>()
			.With<ParticleData>()
			.AsSet()) {
		_defaultTexture = defaultTexture;
		_roomManager = roomManager;
	}

	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {
		ref readonly RoomId room = ref entity.Get<RoomId>();
		if (room.Name == _roomManager.CurrentRoom.Id) {
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