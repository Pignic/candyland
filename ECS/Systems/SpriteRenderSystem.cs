using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.ECS.Systems;

public sealed class SpriteRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Texture2D _baseTexture;

	private Entity[] _visible;
	private float[] _yValues;
	private int[] _sortIndices;
	private int _visibleCount;

	public SpriteRenderSystem(World world, Texture2D baseTexture)
		: base(world.GetEntities()
			.With<RoomActive>()
			.With<Position>()
			.With<Sprite>()
			.AsSet()) {
		_baseTexture = baseTexture;
	}

	protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> entities) {
		EnsureCapacity(ref _visible, entities.Length);
		EnsureCapacity(ref _yValues, entities.Length);
		EnsureCapacity(ref _sortIndices, entities.Length);

		// Filter and extract Y values in one pass
		_visibleCount = 0;
		for (int i = 0; i < entities.Length; i++) {
			Entity entity = entities[i];
			_visible[_visibleCount] = entity;
			_yValues[_visibleCount] = entity.Get<Position>().Value.Y;  // ← Extract Y ONCE
			_sortIndices[_visibleCount] = _visibleCount;
			_visibleCount++;
		}

		// Sort indices by Y values (no component access during sort!)
		Array.Sort(_sortIndices, 0, _visibleCount,
			Comparer<int>.Create((a, b) => _yValues[a].CompareTo(_yValues[b])));

		// Draw in sorted order
		for (int i = 0; i < _visibleCount; i++) {
			int idx = _sortIndices[i];
			Entity entity = _visible[idx];

			DrawSprite(spriteBatch, entity);
			if (entity.Has<Health>() && !entity.Has<DeathAnimation>()) {
				DrawHealthBar(spriteBatch, entity);
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
		if (entity.Has<Animation>()) {
			Animation animation = entity.Get<Animation>();
			sourceRect = animation.GetSourceRect();
		}

		Vector2 origin = sprite.Origin;
		Vector2 size = sprite.TextureSize;
		if (entity.Has<Animation>()) {
			Animation animation = entity.Get<Animation>();
			size.X = animation.FrameWidth;
			size.Y = animation.FrameHeight;
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
			new Rectangle((int)pos.Value.X, (int)pos.Value.Y, (int)(size.X * scale.X), (int)(size.Y * scale.Y)),
			sourceRect,
			tint,
			rotation,
			origin,
			sprite.Effects,
			0f
		);
		if (entity.Has<Collider>()) {
			Collider collider = entity.Get<Collider>();
			spriteBatch.Draw(
				_baseTexture,
				collider.GetBounds(pos),
				Color.Red
			);
		}
	}

	private void DrawHealthBar(SpriteBatch spriteBatch, Entity entity) {
		Health health = entity.Get<Health>();
		if (!health.ShowHealthBar) {
			return;
		}
		Position pos = entity.Get<Position>();

		// Health bar dimensions
		int barWidth = entity.Has<Collider>() ? entity.Get<Collider>().Width : 32;
		int barHeight = 4;
		int barOffsetY = -8;  // Above entity

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
	static void EnsureCapacity<T>(ref T[] array, int size) {
		if (array == null) {
			array = new T[size];
			return;
		}
		if (array.Length < size) {
			int newSize = Math.Max(array.Length * 2, size);
			Array.Resize(ref array, newSize);
		}
	}
}