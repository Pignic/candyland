using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.ECS.Systems;

public sealed class LightRenderSystem : AEntitySetSystem<SpriteBatch> {

	readonly Texture2D _lightTexture;

	public LightRenderSystem(World world, Texture2D lightTexture)
		: base(world.GetEntities()
			.With<RoomActive>()
			.With<CastLight>()
			.With<Position>()
			.With<Collider>()
			.AsSet()) {
		_lightTexture = lightTexture;
	}

	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {
		Position pos = entity.Get<Position>();
		Collider collider = entity.Get<Collider>();
		CastLight castLight = entity.Get<CastLight>();

		float baseAlpha = 0.4f;
		float lightScale = 1f;
		Vector2 shadowOffset = Vector2.Zero;

		if (entity.Has<ZPosition>()) {
			ZPosition z = entity.Get<ZPosition>();
			float heightFactor = z.Z / 50f;

			baseAlpha *= 1f - (heightFactor * 0.7f);
			lightScale *= 1f - (heightFactor * 0.3f);
			shadowOffset = new Vector2(heightFactor * 5f, heightFactor * 5f);
		}

		Rectangle boundingBox = collider.GetBounds(pos);

		Vector2 shadowPos = new Vector2(
			boundingBox.X + shadowOffset.X,
			boundingBox.Y + (boundingBox.Height / 4) + shadowOffset.Y
		);

		Rectangle shadowRect = new Rectangle(
			(int)shadowPos.X,
			(int)shadowPos.Y,
			(int)(collider.Width * lightScale),
			(int)(collider.Height * lightScale)
		);

		spriteBatch.Draw(
			_lightTexture,
			shadowRect,
			castLight.Tint * baseAlpha
		);
	}
}