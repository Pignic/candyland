using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EldmeresTale.World;
using System;

namespace EldmeresTale.Entities; 
public enum EnemyBehavior {
	Idle,           // Stands still
	Patrol,         // Walks back and forth
	Chase,          // Follows the player when in range
	Wander          // Random wandering
}

public class Enemy : ActorEntity {

	// TODO: implement types
	public string EnemyType { get; set; } = "wolf";

	public EnemyBehavior Behavior { get; set; }
	public float DetectionRange { get; set; } = 150f;

	private Vector2 _patrolStart;
	private Vector2 _patrolEnd;
	private Vector2 _targetPosition;
	private bool _movingToEnd = true;

	private float _wanderTimer;
	private float _wanderInterval = 2f;
	private Random _random;

	// Reference to player for chase behavior
	private Entity _chaseTarget;
	private TileMap _map;

	// Drop chances (0.0 to 1.0)
	public float HealthDropChance { get; set; } = 0.3f;
	public float CoinDropChance { get; set; } = 0.8f;
	public bool HasDroppedLoot { get; set; } = false;
	public int XPValue { get; set; } = 50;

	private float _deathTimer = 0f;
	private float _deathRotation = 0f;  // Rotation angle during death
	private float _deathScale = 1f;     // Scale during death
	private float _deathAlpha = 1f;     // Fade out
	private Vector2 _lastAttackerPosition;  // Track for rotation direction

	private const float DEATH_DURATION = 1.0f;       // 1 second death animation
	private const float DEATH_FLASH_DURATION = 0.1f; // 0.1s white flash
	private const float DEATH_SCALE_UP = 1.3f;       // Scale up to 1.3x
	private const float DEATH_ROTATION = 45f;        // 45 degrees

	// Static sprite constructor
	public Enemy(Texture2D texture, Vector2 position, EnemyBehavior behavior, int width = 24, int height = 24, float speed = 100f)
		: base(texture, position, width, height, speed) {
		Behavior = behavior;
		Initialize();
	}

	// Animated sprite constructor
	public Enemy(Texture2D spriteSheet, Vector2 position, EnemyBehavior behavior, int frameCount, int frameWidth, int frameHeight, float frameTime, int width = 24, int height = 24, float speed = 100f)
		: base(spriteSheet, position, frameCount, frameWidth, frameHeight, frameTime, width, height, speed) {
		Behavior = behavior;
		Initialize();
	}

	private void Initialize() {
		_random = new Random(Position.GetHashCode());
		_patrolStart = Position;
		_patrolEnd = Position + new Vector2(100, 0); // Default patrol
		_wanderTimer = _wanderInterval; // Start with full timer so it picks direction immediately
	}

	public override void TakeDamage(int damage, Vector2 attackerPosition) {
		base.TakeDamage(damage, attackerPosition);
		_lastAttackerPosition = attackerPosition;
	}

	protected override void OnDeath() {
		IsDying = true;
		_deathTimer = 0f;

		// Calculate rotation direction based on fatal blow
		Vector2 knockbackDirection = Position - _lastAttackerPosition;
		if(knockbackDirection.Length() > 0) {
			// If hit from the left, rotate right (positive)
			// If hit from the right, rotate left (negative)
			_deathRotation = knockbackDirection.X > 0 ? DEATH_ROTATION : -DEATH_ROTATION;
		} else {
			// Default to random if no direction
			_deathRotation = new Random().NextDouble() > 0.5 ? DEATH_ROTATION : -DEATH_ROTATION;
		}

		System.Diagnostics.Debug.WriteLine($"[ENEMY] Death animation started - rotation: {_deathRotation}°");
	}

	public void SetPatrolPoints(Vector2 start, Vector2 end) {
		_patrolStart = start;
		_patrolEnd = end;
	}

	public void SetChaseTarget(Entity target, TileMap map) {
		_chaseTarget = target;
		_map = map;
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		if(!IsAlive && !IsDying) return;

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;


		if(IsDying) {
			UpdateDeathAnimation(deltaTime);
			return;  // Don't do normal updates when dying
		}

		// Update combat timers (invincibility, knockback)
		UpdateCombatTimers(deltaTime);

		// Check knockback collision
		if(_map != null) {
			ApplyKnockbackWithCollision(_map);
		}


		switch(Behavior) {
			case EnemyBehavior.Idle:
				Velocity = Vector2.Zero;
				break;

			case EnemyBehavior.Patrol:
				UpdatePatrol(deltaTime);
				break;

			case EnemyBehavior.Wander:
				UpdateWander(deltaTime);
				break;

			case EnemyBehavior.Chase:
				if(_chaseTarget != null) {
					UpdateChase(deltaTime);
				}
				break;
		}

		// Update animation
		if(_useAnimation && _animationController != null) {
			_animationController.Update(gameTime, Velocity);
		}
	}

	private void UpdateDeathAnimation(float deltaTime) {
		_deathTimer += deltaTime;

		float progress = _deathTimer / DEATH_DURATION;

		// Phase 1: Flash white (first 0.1s)
		// Phase 2: Scale up + rotate + fade out (remaining time)

		if(_deathTimer < DEATH_FLASH_DURATION) {
			// White flash phase
			_deathScale = 1f;
			_deathAlpha = 1f;
		} else {
			// Scale up + rotate + fade phase
			float animProgress = (_deathTimer - DEATH_FLASH_DURATION) / (DEATH_DURATION - DEATH_FLASH_DURATION);

			// Scale up quickly, then back down
			if(animProgress < 0.3f) {
				_deathScale = MathHelper.Lerp(1f, DEATH_SCALE_UP, animProgress / 0.3f);
			} else {
				_deathScale = MathHelper.Lerp(DEATH_SCALE_UP, 1f, (animProgress - 0.3f) / 0.7f);
			}

			// Fade out
			_deathAlpha = 1f - animProgress;
		}

		// Mark as truly dead when animation completes
		if(_deathTimer >= DEATH_DURATION) {
			health = -999;  // Ensure it's really dead
		}
	}

	private void UpdatePatrol(float deltaTime) {
		Vector2 target = _movingToEnd ? _patrolEnd : _patrolStart;
		Vector2 direction = target - Position;

		if(direction.Length() < 5f) {
			_movingToEnd = !_movingToEnd;
		} else {
			direction.Normalize();
			Velocity = direction * Speed;
			Position += Velocity * deltaTime;
		}
	}

	private void UpdateWander(float deltaTime) {
		_wanderTimer -= deltaTime;

		if(_wanderTimer <= 0) {
			// Pick a new random direction
			float angle = (float)(_random.NextDouble() * Math.PI * 2);
			Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
			Velocity = direction * Speed;

			_wanderTimer = _wanderInterval;
		}

		Position += Velocity * deltaTime;
	}

	private void UpdateChase(float deltaTime) {
		Vector2 targetPosition = _chaseTarget.Position + new Vector2(_chaseTarget.Width / 2f, _chaseTarget.Height / 2f);
		Vector2 direction = targetPosition - Position;
		float distance = direction.Length();

		if(distance > 10f && distance < DetectionRange) {
			direction.Normalize();
			Velocity = direction * Speed;

			Vector2 newPosition = Position + Velocity * deltaTime;

			// Check collision if map provided
			if(_map != null) {
				Rectangle horizontalBounds = new Rectangle((int)newPosition.X, (int)Position.Y, Width, Height);
				if(!_map.checkCollision(horizontalBounds)) {
					Position = new Vector2(newPosition.X, Position.Y);
				}

				Rectangle verticalBounds = new Rectangle((int)Position.X, (int)newPosition.Y, Width, Height);
				if(!_map.checkCollision(verticalBounds)) {
					Position = new Vector2(Position.X, newPosition.Y);
				}
			} else {
				Position = newPosition;
			}
		} else {
			Velocity = Vector2.Zero;
		}
	}

	public void ChaseTarget(Vector2 targetPosition, float deltaTime, TileMap map = null) {
		Vector2 direction = targetPosition - Position;
		float distance = direction.Length();

		if(distance > 10f && distance < DetectionRange) {
			direction.Normalize();
			Velocity = direction * Speed;

			Vector2 newPosition = Position + Velocity * deltaTime;

			// Check collision if map provided
			if(map != null) {
				Rectangle horizontalBounds = new Rectangle((int)newPosition.X, (int)Position.Y, Width, Height);
				if(!map.checkCollision(horizontalBounds)) {
					Position = new Vector2(newPosition.X, Position.Y);
				}

				Rectangle verticalBounds = new Rectangle((int)Position.X, (int)newPosition.Y, Width, Height);
				if(!map.checkCollision(verticalBounds)) {
					Position = new Vector2(Position.X, newPosition.Y);
				}
			} else {
				Position = newPosition;
			}
		} else {
			Velocity = Vector2.Zero;
		}
	}

	public void ApplyCollisionConstraints(TileMap map) {
		// Check if enemy is in a collision and needs to bounce
		if(map != null && map.checkCollision(Bounds)) {
			// For patrol/wander, reverse direction on collision
			if(Behavior == EnemyBehavior.Patrol) {
				_movingToEnd = !_movingToEnd;
			} else if(Behavior == EnemyBehavior.Wander) {
				// Pick a new random direction immediately
				float angle = (float)(_random.NextDouble() * Math.PI * 2);
				Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
				Velocity = direction * Speed;
				_wanderTimer = _wanderInterval;
			}
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		base.Draw(spriteBatch);
		if(_healthBarVisibleTimer > 0f && IsAlive) {
			DrawHealthBar(spriteBatch);
		}
	}

	protected override void DrawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? sourceRect, Color tint) {
		if(IsDying) {
			Vector2 origin = sourceRect.HasValue
				? new Vector2(sourceRect.Value.Width / 2f, sourceRect.Value.Height / 2f)
				: new Vector2(texture.Width / 2f, texture.Height / 2f);

			// Adjust position to account for origin
			Vector2 drawPosition = position + origin;

			// Convert rotation to radians
			float rotationRadians = MathHelper.ToRadians(_deathRotation * (_deathTimer / DEATH_DURATION));

			// Draw with rotation and scale
			spriteBatch.Draw(
				texture,
				drawPosition,
				sourceRect,
				tint,
				rotationRadians,
				origin,
				_deathScale,
				SpriteEffects.None,
				0f
			);
		} else {
			base.DrawSprite(spriteBatch, texture, position, sourceRect, tint);
		}
	}

	protected override Color getTint() {
		Color tint = base.getTint();

		// Death animation overrides
		if(IsDying) {
			// Phase 1: White flash
			if(_deathTimer < DEATH_FLASH_DURATION) {
				tint = Color.White;
			} else {
				// Phase 2: Normal color but fading
				tint = tint * _deathAlpha;
			}
		}
		return tint;
	}
}