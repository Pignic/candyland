using EldmeresTale.Core;
using EldmeresTale.Events;
using EldmeresTale.Systems;
using EldmeresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Entities;

public class Player : ActorEntity {
	// Stats system
	public PlayerStats Stats { get; set; }

	private GameEventBus _eventBus;

	// Inventory system
	public Inventory Inventory { get; private set; }

	private float _speedMultiplier = 1f;
	private const float ATTACK_SPEED_MULTIPLIER = 0.15f;
	private const float SPEED_TRANSITION_RATE = 15f;

	// dodging 
	private bool _isDodging = false;
	private float _dodgeTimer = 0f;
	private float _dodgeCooldown = 0f;
	private Vector2 _dodgeDirection = Vector2.Zero;

	private const float DODGE_DURATION = 0.2f;         // How long the dash lasts
	private const float DODGE_SPEED = 500f;            // Speed during dodge
	private const float DODGE_COOLDOWN = 1.0f;         // Cooldown between dodges
	private const float DODGE_IFRAMES = 0.3f;          // Invincibility duration
	public bool CanDodge => !_isDodging && !_isAttacking && _dodgeCooldown <= 0f;

	// dodge fx
	private struct TrailFrame {
		public Vector2 Position;
		public float Alpha;
		public Rectangle? SourceRect;  // For animated sprites
	}
	private List<TrailFrame> _dodgeTrail = new List<TrailFrame>();
	private const int MAX_TRAIL_FRAMES = 5;
	private const float TRAIL_SPAWN_INTERVAL = 0.03f;  // Spawn trail every 0.03 seconds
	private float _trailSpawnTimer = 0f;


	// Attack properties
	private float _attackCooldown = 0f;
	private float _attackRange => Stats.AttackRange;
	private bool _isAttacking = false;
	private float _attackDuration = 0.2f;
	private float _attackTimer = 0f;
	private Vector2 _lastMoveDirection = new Vector2(0, 1); // Default: down

	// Track which enemies have been hit this attack
	private HashSet<Entity> _hitThisAttack = new HashSet<Entity>();

	// Attack effect
	private AttackEffect _attackEffect;

	// Health regeneration timer
	private float _regenTimer = 0f;
	private const float REGEN_TICK_RATE = 1f; // Regen applies every second

	// Random for crit/dodge calculations
	private Random _random;

	public bool CanAttack => _attackCooldown <= 0 && !_isAttacking;

	// Player progression
	public int Coins { get; set; } = 0;
	public int Level { get; set; } = 1;
	public int XP { get; set; } = 0;
	public int XPToNextLevel { get; set; } = 100;

	public event Action OnPlayerDeath;
	public event Action<Vector2> OnDodge;

	// Override base properties to use Stats
	public new int MaxHealth => Stats.MaxHealth;
	public new int AttackDamage => Stats.AttackDamage;
	public new float Speed => Stats.Speed;

	// Attack hitbox (in front of player)
	public Rectangle AttackBounds {
		get {
			if (!_isAttacking) {
				return Rectangle.Empty;
			}

			Vector2 center = Position + new Vector2(Width / 2f, Height / 2f);
			float hitboxSize = _attackRange;

			Vector2 attackOffset = _lastMoveDirection * _attackRange;
			Vector2 attackPos = center + attackOffset;

			return new Rectangle(
				(int)(attackPos.X - (hitboxSize / 2)),
				(int)(attackPos.Y - (hitboxSize / 2)),
				(int)hitboxSize,
				(int)hitboxSize
			);
		}
	}

	// Constructor for static sprite
	public Player(Texture2D texture, Vector2 startPosition, int width = 16, int height = 16)
		: base(texture, startPosition, width, height, 200f) {
		InitializePlayer();
	}

	// Constructor for animated sprite
	public Player(Texture2D spriteSheet, Vector2 startPosition, int frameCount, int frameWidth, int frameHeight, float frameTime, int width = 16, int height = 16)
		: base(spriteSheet, startPosition, frameCount, frameWidth, frameHeight, frameTime, width, height, 200f, true) {
		InitializePlayer();
	}
	public void SetEventBus(GameEventBus eventBus) {
		_eventBus = eventBus;
	}

	private void InitializePlayer() {
		Stats = new PlayerStats();
		Inventory = new Inventory(maxSize: 50); // 50 item limit
		health = Stats.MaxHealth;
		Level = 1;
		XP = 0;
		XPToNextLevel = 100;
		_random = new Random();
		_dodgeTrail = new List<TrailFrame>();
	}

	public void InitializeAttackEffect(Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice) {
		_attackEffect = new AttackEffect(graphicsDevice);
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
	}

	public void Update(GameTime gameTime, TileMap map, InputCommands input) {
		base.Update(gameTime);
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		// Update combat timers
		UpdateCombatTimers(deltaTime);

		// Check knockback collision
		ApplyKnockbackWithCollision(map, deltaTime);
		if (!IsDying) {

			// Update attack cooldown (now based on attack speed)
			if (_attackCooldown > 0f) {
				_attackCooldown -= deltaTime;
			}

			if (_dodgeCooldown > 0f) {
				_dodgeCooldown -= deltaTime;
			}

			if (_isDodging) {
				_dodgeTimer -= deltaTime;

				_trailSpawnTimer += deltaTime;
				if (_trailSpawnTimer >= TRAIL_SPAWN_INTERVAL) {
					_trailSpawnTimer = 0f;

					// Add new trail frame
					TrailFrame frame = new TrailFrame {
						Position = Position,
						Alpha = 0.6f,
						SourceRect = _useAnimation ? _animationController?.GetSourceRectangle() : null
					};
					_dodgeTrail.Add(frame);

					// Remove old frames
					if (_dodgeTrail.Count > MAX_TRAIL_FRAMES) {
						_dodgeTrail.RemoveAt(0);
					}
				}

				if (_dodgeTimer <= 0f) {
					_isDodging = false;
				}
			}
			if (!_isDodging && _dodgeTrail.Count > 0) {
				// Fade all trail frames
				for (int i = 0; i < _dodgeTrail.Count; i++) {
					TrailFrame frame = _dodgeTrail[i];
					frame.Alpha -= deltaTime * 3f;  // Fade out quickly
					_dodgeTrail[i] = frame;
				}

				// Remove fully faded frames
				_dodgeTrail.RemoveAll(f => f.Alpha <= 0f);
			}

			// Update attack animation

			float targetSpeedMultiplier = _isAttacking ? ATTACK_SPEED_MULTIPLIER : 1f;
			_speedMultiplier = MathHelper.Lerp(_speedMultiplier, targetSpeedMultiplier, SPEED_TRANSITION_RATE * deltaTime);
			if (_isAttacking) {
				_attackTimer -= deltaTime;
				if (_attackTimer <= 0) {
					_isAttacking = false;
					_hitThisAttack.Clear();
				}
			}

			// Apply health regeneration
			ApplyHealthRegen(deltaTime);

			// Update attack effect
			if (_attackEffect != null) {
				_attackEffect.Update(gameTime);
			}

			HandleInput(input, deltaTime, map);

		}

		// Update animation if using one
		if (_useAnimation && _animationController != null) {
			_animationController.Update(gameTime, Velocity);
		}
	}
	protected override void OnDeath() {
		if (IsDying) {
			return;  // Already dying
		}

		IsDying = true;
		System.Diagnostics.Debug.WriteLine("[PLAYER] Death!");

		// Fire death event for game to handle
		OnPlayerDeath?.Invoke();
		_eventBus?.Publish(new PlayerDeathEvent {
			DeathPosition = Position,
			Position = Position
		});
	}

	private void ApplyHealthRegen(float deltaTime) {
		if (Stats.HealthRegen <= 0 || health >= Stats.MaxHealth) {
			return;
		}

		_regenTimer += deltaTime;

		if (_regenTimer >= REGEN_TICK_RATE) {
			_regenTimer -= REGEN_TICK_RATE;
			health = Math.Min(health + (int)Stats.HealthRegen, Stats.MaxHealth);
		}
	}

	public void Attack() {
		if (CanAttack) {
			_isAttacking = true;
			_attackTimer = _attackDuration;
			_attackCooldown = Stats.AttackCooldownDuration; // Use stats-based cooldown
			_hitThisAttack.Clear();

			// Trigger visual slash effect
			if (_attackEffect != null) {
				_attackEffect.Trigger(() => Position + new Vector2(Width / 2f, Height / 2f), _lastMoveDirection, _attackRange);
			}
			_eventBus?.Publish(new PlayerAttackEvent {
				Actor = this
			});
			base.InvokeAttackEvent();
		}
	}
	public void Dodge() {
		if (!CanDodge) {
			return;
		}

		_isDodging = true;
		_dodgeTimer = DODGE_DURATION;
		_dodgeCooldown = DODGE_COOLDOWN;
		_trailSpawnTimer = 0f;
		_dodgeTrail.Clear();

		// Dodge in movement direction, or backward if standing still
		if (_lastMoveDirection.Length() > 0) {
			_dodgeDirection = _lastMoveDirection;
		} else {
			// If standing still, dodge backward (away from last direction)
			_dodgeDirection = -_lastMoveDirection;
		}

		_dodgeDirection.Normalize();

		// Grant invincibility frames (longer than dodge duration!)
		_invincibilityTimer = DODGE_IFRAMES;
		OnDodge?.Invoke(_dodgeDirection);
		_eventBus?.Publish(new PlayerDodgeEvent {
			DodgeDirection = _dodgeDirection,
			Position = Position
		});

		System.Diagnostics.Debug.WriteLine($"[DODGE] Rolling! Direction: {_dodgeDirection}");
	}

	public void DrawAttackEffect(SpriteBatch spriteBatch) {
		if (_attackEffect != null) {
			_attackEffect.Draw(spriteBatch);
		}
	}

	public bool HasHitEntity(Entity entity) {
		return _hitThisAttack.Contains(entity);
	}

	public void MarkEntityAsHit(Entity entity) {
		_hitThisAttack.Add(entity);
	}

	// Calculate damage with crit and return if it was a crit
	public (int damage, bool wasCrit) CalculateDamage() {
		bool isCrit = Stats.RollCritical(_random);
		int damage = isCrit ? (int)(Stats.AttackDamage * Stats.CritMultiplier) : Stats.AttackDamage;
		return (damage, isCrit);
	}

	public override void TakeDamage(int damage, Vector2 attackerPosition) {
		if (IsInvincible || !IsAlive) {
			return;
		}

		// Check for dodge
		if (Stats.RollDodge(_random)) {
			// Dodged! No damage taken
			System.Diagnostics.Debug.WriteLine("Dodged!");

			// Still apply invincibility frames to prevent spam
			_invincibilityTimer = _invincibilityDuration;
			return;
		}

		// Apply defense reduction
		int reducedDamage = Stats.CalculateDamageReduction(damage);

		health -= reducedDamage;
		if (health < 0) {
			health = 0;
		}

		// Apply knockback away from attacker
		Vector2 knockbackDirection = Position - attackerPosition;
		if (knockbackDirection.Length() > 0) {
			knockbackDirection.Normalize();
			_knockbackVelocity = knockbackDirection * 300f;
		}

		// Start invincibility frames
		_invincibilityTimer = _invincibilityDuration;

		if (!IsAlive) {
			OnDeath();
		}
	}

	// Called when player damages an enemy - apply lifesteal
	public void OnDamageDealt(int damageDealt) {
		if (Stats.LifeSteal > 0) {
			int healAmount = (int)(damageDealt * Stats.LifeSteal);
			if (healAmount > 0) {
				health = Math.Min(health + healAmount, Stats.MaxHealth);
			}
		}
	}

	private void HandleInput(InputCommands input, float deltaTime, TileMap map = null) {
		if (IsDying) {
			Velocity = Vector2.Zero;  // Stop moving (but knockback still works!)
			return;
		}
		// Attack input
		if (input.AttackHeld) {  // Use InputCommands!
			Attack();
		}
		if (input.DodgePressed) {
			Dodge();
		}

		// Get movement from InputCommands (already normalized!)
		Vector2 movement = input.Movement;

		// Update last move direction if moving
		if (movement.Length() > 0) {
			_lastMoveDirection = movement;
		}

		// Calculate velocity (use stats speed)
		if (_isDodging) {
			// During dodge, move at high speed in dodge direction
			Velocity = _dodgeDirection * DODGE_SPEED;
		} else {
			// Normal movement
			Velocity = movement * Stats.Speed * _speedMultiplier;
		}

		// Apply movement with collision detection
		Vector2 newPosition = Position + (Velocity * deltaTime);

		// If we have a map, check collision
		if (map != null) {
			Vector2 desiredMovement = Velocity * deltaTime;

			// Resolve collision and move
			TileMap.MovementResult result = map.ResolveMovement(Bounds, desiredMovement);
			Position += result.Movement;
		} else {
			Position = newPosition;
		}
	}

	public bool GainXP(int amount) {
		XP += amount;

		if (XP >= XPToNextLevel) {
			LevelUp();
			_eventBus?.Publish(new PlayerLevelUpEvent {
				NewLevel = Level,
				XpGained = amount,
				Position = Position
			});
			return true;
		}

		return false;
	}

	private void LevelUp() {
		Level++;
		XP -= XPToNextLevel;

		// Increase XP requirement for next level
		XPToNextLevel = (int)(XPToNextLevel * 1.5f);

		// Apply stat bonuses through the stats system
		Stats.ApplyLevelUpBonus();

		// Fully heal on level up
		health = Stats.MaxHealth;
	}

	public void ClampToScreen(int screenWidth, int screenHeight) {
		Position = new Vector2(
			MathHelper.Clamp(Position.X, 0, screenWidth - Width),
			MathHelper.Clamp(Position.Y, 0, screenHeight - Height)
		);
	}

	public void reset() {
		health = Stats.MaxHealth;
		XP = 0;
		Level = 1;
		Coins = 0;

		// Clear inventory
		Inventory = new Inventory();
	}

	protected override Color getTint() {
		if (_isDodging) {
			return Color.White * 0.7f;
		}
		return base.getTint();
	}

	public void CollectPickup(Pickup pickup) {
		if (pickup.HealthRestore > 0) {
			health = Math.Min(health + pickup.HealthRestore, Stats.MaxHealth);
		}

		if (pickup.CoinValue > 0) {
			Coins += pickup.CoinValue;
		}

		pickup.Collect();
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if (_isDodging && _dodgeTrail.Count > 0) {
			DrawDodgeTrail(spriteBatch);
		}
		bool drawEffectBehind = ShouldDrawAttackEffectBehind();

		// Attack effect BEFORE player if attacking up
		if (drawEffectBehind && _attackEffect != null) {
			_attackEffect.Draw(spriteBatch);
		}

		base.Draw(spriteBatch);  // Player sprite

		// Attack effect AFTER player if attacking down
		if (!drawEffectBehind && _attackEffect != null) {
			_attackEffect.Draw(spriteBatch);
		}
	}
	private bool ShouldDrawAttackEffectBehind() {
		return _lastMoveDirection.Y < -0.3f;  // True if attacking up
	}

	private void DrawDodgeTrail(SpriteBatch spriteBatch) {
		foreach (TrailFrame frame in _dodgeTrail) {
			Color trailTint = Color.White * frame.Alpha;

			if (_useAnimation && frame.SourceRect.HasValue) {
				// Draw animated sprite trail
				Vector2 spritePosition = new Vector2(
					frame.Position.X + ((Width - frame.SourceRect.Value.Width) / 2f),
					frame.Position.Y + ((Height - frame.SourceRect.Value.Height) / 2f)
				);
				spriteBatch.Draw(_animationController.GetTexture(), spritePosition, frame.SourceRect.Value, trailTint);
			} else {
				// Draw static sprite trail
				Vector2 spritePosition = new Vector2(
					frame.Position.X + ((Width - _texture.Width) / 2f),
					frame.Position.Y + ((Height - _texture.Height) / 2f)
				);
				spriteBatch.Draw(_texture, spritePosition, trailTint);
			}
		}
	}
}