using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.ECS.Systems;

public sealed class ShadowRenderSystem : AEntitySetSystem<SpriteBatch> {

	readonly Texture2D _shadowTexture;

	public ShadowRenderSystem(World world, Texture2D shadowTexture)
		: base(world.GetEntities()
			.With<RoomActive>()
			.With<CastShadow>()
			.With<Position>()
			.With<Collider>()
			.AsSet()) {
		_shadowTexture = shadowTexture;
	}

	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {
		Position pos = entity.Get<Position>();
		Collider collider = entity.Get<Collider>();
		CastShadow castShadow = entity.Get<CastShadow>();

		float baseAlpha = 0.4f;
		float shadowScale = 1f;
		Vector2 shadowOffset = Vector2.Zero;

		if (entity.Has<ZPosition>()) {
			ZPosition z = entity.Get<ZPosition>();
			float heightFactor = z.Z / 50f;

			baseAlpha *= 1f - (heightFactor * 0.7f);
			shadowScale *= 1f - (heightFactor * 0.3f);
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
			(int)(collider.Width * shadowScale),
			(int)(collider.Height * shadowScale)
		);

		spriteBatch.Draw(
			_shadowTexture,
			shadowRect,
			castShadow.Tint * baseAlpha
		);
	}
}