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
	private const float HEALTH_BAR_FADE_DURATION = 0.5f;

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

	protected void DrawHealthBar(SpriteBatch spriteBatch) {
		// Health bar dimensions
		int barWidth = Width;
		const int barHeight = 3;
		const int barOffsetY = -8;  // Above enemy

		// Calculate alpha (fade out in last 0.5 seconds)
		float alpha = 1f;
		if (_healthBarVisibleTimer < HEALTH_BAR_FADE_DURATION) {
			alpha = _healthBarVisibleTimer / HEALTH_BAR_FADE_DURATION;
		}

		// Position above enemy
		Vector2 barPosition = new Vector2(
			Position.X,
			Position.Y + barOffsetY
		);

		// Background (dark gray)
		Rectangle bgRect = new Rectangle(
			(int)barPosition.X,
			(int)barPosition.Y,
			barWidth,
			barHeight
		);

		// Foreground (health - red to green gradient)
		float healthPercent = (float)Health / MaxHealth;
		int foregroundWidth = (int)(barWidth * healthPercent);

		Rectangle fgRect = new Rectangle(
			(int)barPosition.X,
			(int)barPosition.Y,
			foregroundWidth,
			barHeight
		);

		// Color based on health percentage
		Color healthColor;
		if (healthPercent > 0.6f) {
			healthColor = Color.LimeGreen;  // Healthy
		} else if (healthPercent > 0.3f) {
			healthColor = Color.Yellow;     // Wounded
		} else {
			healthColor = Color.Red;        // Critical
		}

		// Apply fade
		Color bgColor = Color.Black * alpha;
		healthColor *= alpha;

		// Draw (use a simple pixel texture or create one)
		// You'll need a white pixel texture - see next step
		Texture2D pixel = GetWhitePixelTexture(spriteBatch.GraphicsDevice);

		spriteBatch.Draw(pixel, bgRect, bgColor);
		spriteBatch.Draw(pixel, fgRect, healthColor);
	}

	// Helper method to get/create white pixel texture
	private static Texture2D _whitePixel = null;

	private static Texture2D GetWhitePixelTexture(GraphicsDevice graphicsDevice) {
		if (_whitePixel == null) {
			_whitePixel = new Texture2D(graphicsDevice, 1, 1);
			_whitePixel.SetData(new[] { Color.White });
		}
		return _whitePixel;
	}

	protected override bool RequireDrawing() {
		return IsAlive || IsDying;
	}
}
