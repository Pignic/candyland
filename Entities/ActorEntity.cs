using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Entities;

public abstract class ActorEntity : BaseEntity {

	public Vector2 PreviousPosition { get; set; }

	public bool IsDying { get; protected set; } = false;

	protected float _healthBarVisibleTimer = 0f;
	private const float HEALTH_BAR_VISIBLE_DURATION = 2.0f;

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
		if (_invincibilityTimer > 0) {
			_invincibilityTimer -= deltaTime;
		}
	}

	protected void InvokeAttackEvent() {
		OnAttack?.Invoke(this);
	}

	public override void TakeDamage(int damage, Vector2 attackerPosition) {
		base.TakeDamage(damage, attackerPosition);
		_healthBarVisibleTimer = HEALTH_BAR_VISIBLE_DURATION;
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		PreviousPosition = new Vector2(base.Position.X, base.Position.Y);
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		if (_healthBarVisibleTimer > 0f) {
			_healthBarVisibleTimer -= deltaTime;
		}
	}

	// Apply knockback with collision checking, return collisionNormal (if any)
	public Vector2? ApplyKnockbackWithCollision(TileMap map, float deltaTime) {
		if (_knockbackVelocity.Length() == 0 || map == null) {
			return null;
		}

		Vector2 knockbackMovement = _knockbackVelocity * deltaTime;
		TileMap.MovementResult result = map.ResolveMovement(Bounds, knockbackMovement);

		Position += result.Movement;

		// Knockback velocity is reduced by collision!
		_knockbackVelocity -= result.BlockedVelocity / deltaTime;
		_knockbackVelocity -= _knockbackVelocity * _knockbackDecay * deltaTime;

		if (_knockbackVelocity.Length() < 1f) {
			_knockbackVelocity = Vector2.Zero;
		}

		// If hit a wall, lose knockback faster (impact absorbs energy)
		if (result.WasBlocked) {
			// Cut knockback duration in half
			_knockbackVelocity *= 0.5f;
			// Spawn impact particles at wall
			return result.CollisionNormal;
		}
		return null;
	}

	protected override bool RequireDrawing() {
		return IsAlive || IsDying;
	}
}
