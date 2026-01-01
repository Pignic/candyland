using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Entities;

public abstract class ActorEntity : Entity {

	public Vector2 PreviousPosition { get; set; }

	public event Action<ActorEntity> OnAttack;
	public event Action<ActorEntity> OnAttacked;

	protected ActorEntity(Texture2D texture, Vector2 position, int width, int height, float speed) : base(texture, position, width, height, speed) {
		PreviousPosition = new Vector2(base.Position.X, base.Position.Y);
	}

	protected ActorEntity(Texture2D spriteSheet, Vector2 position, int frameCount, int frameWidth, int frameHeight, float frameTime, int width, int height, float speed, bool pingpong = false) : base(spriteSheet, position, frameCount, frameWidth, frameHeight, frameTime, width, height, speed, pingpong) {
		PreviousPosition = new Vector2(position.X, position.Y);
	}

	protected void UpdateCombatTimers(float deltaTime) {
		// Update invincibility timer
		if(_invincibilityTimer > 0) {
			_invincibilityTimer -= deltaTime;
		}

		// Apply and decay knockback
		if(_knockbackVelocity.Length() > 0) {
			Position += _knockbackVelocity * deltaTime;
			_knockbackVelocity -= _knockbackVelocity * _knockbackDecay * deltaTime;

			if(_knockbackVelocity.Length() < 1f) {
				_knockbackVelocity = Vector2.Zero;
			}
		}
	}

	protected void InvokeAttackEvent() {
		OnAttack?.Invoke(this);
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		PreviousPosition = new Vector2(base.Position.X, base.Position.Y);
	}

	// Apply knockback with collision checking
	public void ApplyKnockbackWithCollision(World.TileMap map) {
		if(_knockbackVelocity.Length() == 0 || map == null)
			return;

		// Check if knockback position would collide
		Rectangle potentialBounds = new Rectangle(
			(int)Position.X,
			(int)Position.Y,
			Width,
			Height
		);

		if(map.checkCollision(potentialBounds)) {
			// Cancel knockback if it would push into a wall
			_knockbackVelocity = Vector2.Zero;
		}
	}
}
