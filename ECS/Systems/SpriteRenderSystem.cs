using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace EldmeresTale.ECS.Systems;

public sealed class SpriteRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Camera _camera;
	private readonly BitmapFont _font;
	private readonly Texture2D _baseTexture;

	public SpriteRenderSystem(World world, Camera camera, BitmapFont font, Texture2D baseTexture)
		: base(world.GetEntities()
			.With<Position>()
			.With<Sprite>()
			.AsSet()) {
		_camera = camera;
		_font = font;
		_baseTexture = baseTexture;
	}

	protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> entities) {
		// Sort by Y for depth
		IOrderedEnumerable<Entity> sorted = entities.ToArray()
			.OrderBy(e => e.Get<Position>().Value.Y);

		foreach (Entity entity in sorted) {
			DrawSprite(spriteBatch, entity);
			if (entity.Has<Health>() && !entity.Has<DeathAnimation>()) {
				DrawHealthBar(spriteBatch, entity);
			}
		}

		// Draw interaction prompts on top
		foreach (Entity entity in entities) {
			if (entity.Has<InteractionZone>()) {
				DrawInteractionPrompt(spriteBatch, entity);
			}
		}
	}

	private void DrawSprite(SpriteBatch spriteBatch, Entity entity) {
		Position pos = entity.Get<Position>();
		Sprite sprite = entity.Get<Sprite>();

		if (sprite.Texture == null) {
			return;
		}

		// Flash white if invincible
		Color tint = sprite.Tint;
		if (entity.Has<Health>()) {
			Health health = entity.Get<Health>();
			if (health.IsInvincible && !health.IsDead) {
				// Flash effect
				float flashAlpha = MathF.Sin(health.InvincibilityTimer * 20f);
				if (flashAlpha > 0) {
					tint = Color.White;
				}
			}
		}

		Rectangle? sourceRect = sprite.SourceRect;
		if (entity.Has<Components.Animation>()) {
			Components.Animation animation = entity.Get<Components.Animation>();
			sourceRect = animation.GetSourceRect();
		}

		Vector2 origin = sprite.Origin;
		if (entity.Has<Collider>()) {
			Collider collider = entity.Get<Collider>();
			origin = new Vector2(collider.Width / 2, collider.Height / 2);
		}

		Vector2 scale = sprite.Scale;
		float rotation = sprite.Rotation;
		if (entity.Has<DeathAnimation>()) {
			DeathAnimation deathAnimation = entity.Get<DeathAnimation>();
			scale.X = deathAnimation.CurrentScale;
			scale.Y = deathAnimation.CurrentScale;
			rotation = deathAnimation.CurrentRotation;
		}

		spriteBatch.Draw(
			sprite.Texture,
			// TODO: remove the 12,12, it for debug
			pos.Value + origin + new Vector2(12, 12),
			sourceRect,
			tint,
			rotation,
			origin,
			scale,
			sprite.Effects,
			0f
		);
	}

	private void DrawInteractionPrompt(SpriteBatch spriteBatch, Entity entity) {
		InteractionZone zone = entity.Get<InteractionZone>();

		if (!zone.IsPlayerNearby) {
			return;
		}

		Position pos = entity.Get<Position>();

		// Draw "E" indicator above prop
		string text = "E";
		int textWidth = _font.MeasureString(text);
		int textHeight = _font.GetHeight();

		Vector2 textPos = new Vector2(
			pos.Value.X + 16 - (textWidth / 2),  // Center over 32x32 prop
			pos.Value.Y - 20  // Above prop
		);

		// Draw with slight bob effect
		float bobOffset = MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * 3f) * 2f;
		textPos.Y += bobOffset;

		_font.DrawText(spriteBatch, text, textPos, Color.Yellow, scale: 2);
	}

	private void DrawHealthBar(SpriteBatch spriteBatch, Entity entity) {
		Position pos = entity.Get<Position>();
		Health health = entity.Get<Health>();
		Collider collider = entity.Has<Collider>() ? entity.Get<Collider>() : new Collider(32, 32);

		// Health bar dimensions
		int barWidth = collider.Width;
		int barHeight = 4;
		int barOffsetY = -8;  // Above enemy

		Vector2 barPosition = new Vector2(
			pos.Value.X,
			pos.Value.Y + barOffsetY
		);

		// Background (red)
		Rectangle bgRect = new Rectangle(
			(int)barPosition.X,
			(int)barPosition.Y,
			barWidth,
			barHeight
		);
		spriteBatch.Draw(_baseTexture, bgRect, Color.DarkRed);

		// Foreground (green)
		int healthWidth = (int)(barWidth * health.HealthRatio);
		if (healthWidth > 0) {
			Rectangle fgRect = new Rectangle(
				(int)barPosition.X,
				(int)barPosition.Y,
				healthWidth,
				barHeight
			);

			// Color based on health
			Color healthColor = health.HealthRatio > 0.5f ? Color.LimeGreen :
								health.HealthRatio > 0.25f ? Color.Yellow :
								Color.Red;

			spriteBatch.Draw(_baseTexture, fgRect, healthColor);
		}
	}
}