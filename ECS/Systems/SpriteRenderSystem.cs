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

	private Entity[] _buffer;
	private Entity[] _visible;
	private int _visibleCount;

	readonly RoomManager _roomManager;

	public SpriteRenderSystem(World world, Texture2D baseTexture, RoomManager roomManager)
		: base(world.GetEntities()
			.With<Position>()
			.With<Sprite>()
			.With<RoomId>()
			.AsSet()) {
		_baseTexture = baseTexture;
		_roomManager = roomManager;
	}


	private static readonly IComparer<Entity> YComparer =
		Comparer<Entity>.Create((a, b) =>
			a.Get<Position>().Value.Y.CompareTo(b.Get<Position>().Value.Y));

	protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> entities) {
		EnsureCapacity(ref _buffer, entities.Length);
		EnsureCapacity(ref _visible, entities.Length);

		// Filter
		_visibleCount = 0;
		for (int i = 0; i < entities.Length; i++) {
			ref readonly RoomId room = ref entities[i].Get<RoomId>();
			if (room.Name == _roomManager.CurrentRoom.Id) {
				_visible[_visibleCount++] = entities[i];
			}
		}

		// Sort visible only
		Array.Sort(_visible, 0, _visibleCount, YComparer);

		// Draw
		for (int i = 0; i < _visibleCount; i++) {
			DrawSprite(spriteBatch, _visible[i]);
			if (_visible[i].Has<Health>() && !_visible[i].Has<DeathAnimation>()) {
				DrawHealthBar(spriteBatch, _visible[i]);
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
			pos.Value + origin,
			sourceRect,
			tint,
			rotation,
			origin,
			scale,
			sprite.Effects,
			0f
		);
	}

	private void DrawHealthBar(SpriteBatch spriteBatch, Entity entity) {
		Health health = entity.Get<Health>();
		if (!health.ShowHealthBar) {
			return;
		}
		Position pos = entity.Get<Position>();
		Collider collider = entity.Has<Collider>() ? entity.Get<Collider>() : new Collider(32, 32);

		// Health bar dimensions
		int barWidth = collider.Width;
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