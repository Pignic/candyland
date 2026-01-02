using EldmeresTale.Core;
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

	public event Action OnDodge;

	// Inventory system
	public Inventory Inventory { get; private set; }

	private float _speedMultiplier = 1f;
	private const float ATTACK_SPEED_MULTIPLIER = 0.25f;
	private const float SPEED_TRANSITION_RATE = 12f;

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

	// Override base properties to use Stats
	public new int MaxHealth => Stats.MaxHealth;
	public new int AttackDamage => Stats.AttackDamage;
	public new float Speed => Stats.Speed;

	// Attack hitbox (in front of player)
	public Rectangle AttackBounds {
		get {
			if(!_isAttacking)
				return Rectangle.Empty;

			Vector2 center = Position + new Vector2(Width / 2f, Height / 2f);
			float hitboxSize = _attackRange;

			Vector2 attackOffset = _lastMoveDirection * _attackRange;
			Vector2 attackPos = center + attackOffset;

			return new Rectangle(
				(int)(attackPos.X - hitboxSize / 2),
				(int)(attackPos.Y - hitboxSize / 2),
				(int)(hitboxSize),
				(int)(hitboxSize)
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

	private void InitializePlayer() {
		Stats = new PlayerStats();
		Inventory = new Inventory(maxSize: 50); // 50 item limit
		health = Stats.MaxHealth;
		Level = 1;
		XP = 0;
		XPToNextLevel = 100;
		_random = new Random();
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
		ApplyKnockbackWithCollision(map);

		// Update attack cooldown (now based on attack speed)
		if(_attackCooldown > 0f)
			_attackCooldown -= deltaTime;

		if(_dodgeCooldown > 0f)
			_dodgeCooldown -= deltaTime;

		if(_isDodging) {
			_dodgeTimer -= deltaTime;
			if(_dodgeTimer <= 0f) {
				_isDodging = false;
			}
		}

		// Update attack animation

		float targetSpeedMultiplier = _isAttacking ? ATTACK_SPEED_MULTIPLIER : 1f;
		_speedMultiplier = MathHelper.Lerp(_speedMultiplier, targetSpeedMultiplier, SPEED_TRANSITION_RATE * deltaTime);
		if(_isAttacking) {
			_attackTimer -= deltaTime;
			if(_attackTimer <= 0) {
				_isAttacking = false;
				_hitThisAttack.Clear();
			}
		}

		// Apply health regeneration
		ApplyHealthRegen(deltaTime);

		// Update attack effect
		if(_attackEffect != null) {
			_attackEffect.Update(gameTime);
		}

		HandleInput(input, deltaTime, map);

		// Update animation if using one
		if(_useAnimation && _animationController != null) {
			_animationController.Update(gameTime, Velocity);
		}
	}

	private void ApplyHealthRegen(float deltaTime) {
		if(Stats.HealthRegen <= 0 || health >= Stats.MaxHealth)
			return;

		_regenTimer += deltaTime;

		if(_regenTimer >= REGEN_TICK_RATE) {
			_regenTimer -= REGEN_TICK_RATE;
			health = Math.Min(health + (int)Stats.HealthRegen, Stats.MaxHealth);
		}
	}

	public void Attack() {
		if(CanAttack) {
			_isAttacking = true;
			_attackTimer = _attackDuration;
			_attackCooldown = Stats.AttackCooldownDuration; // Use stats-based cooldown
			_hitThisAttack.Clear();

			// Trigger visual slash effect
			if(_attackEffect != null) {
				Vector2 attackPos = Position + new Vector2(Width / 2f, Height / 2f);
				attackPos += _lastMoveDirection * _attackRange;
				_attackEffect.Trigger(attackPos, _lastMoveDirection);
			}
			base.InvokeAttackEvent();
		}
	}
	public void Dodge() {
		if(!CanDodge) return;

		_isDodging = true;
		_dodgeTimer = DODGE_DURATION;
		_dodgeCooldown = DODGE_COOLDOWN;

		// Dodge in movement direction, or backward if standing still
		if(_lastMoveDirection.Length() > 0) {
			_dodgeDirection = _lastMoveDirection;
		} else {
			// If standing still, dodge backward (away from last direction)
			_dodgeDirection = -_lastMoveDirection;
		}

		_dodgeDirection.Normalize();

		// Grant invincibility frames (longer than dodge duration!)
		_invincibilityTimer = DODGE_IFRAMES;
		OnDodge?.Invoke();

		System.Diagnostics.Debug.WriteLine($"[DODGE] Rolling! Direction: {_dodgeDirection}");
	}

	public void DrawAttackEffect(SpriteBatch spriteBatch) {
		if(_attackEffect != null) {
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
		if(IsInvincible || !IsAlive)
			return;

		// Check for dodge
		if(Stats.RollDodge(_random)) {
			// Dodged! No damage taken
			System.Diagnostics.Debug.WriteLine("Dodged!");

			// Still apply invincibility frames to prevent spam
			_invincibilityTimer = _invincibilityDuration;
			return;
		}

		// Apply defense reduction
		int reducedDamage = Stats.CalculateDamageReduction(damage);

		health -= reducedDamage;
		if(health < 0)
			health = 0;

		// Apply knockback away from attacker
		Vector2 knockbackDirection = Position - attackerPosition;
		if(knockbackDirection.Length() > 0) {
			knockbackDirection.Normalize();
			_knockbackVelocity = knockbackDirection * 300f;
		}

		// Start invincibility frames
		_invincibilityTimer = _invincibilityDuration;

		if(!IsAlive) {
			OnDeath();
		}
	}

	// Called when player damages an enemy - apply lifesteal
	public void OnDamageDealt(int damageDealt) {
		if(Stats.LifeSteal > 0) {
			int healAmount = (int)(damageDealt * Stats.LifeSteal);
			if(healAmount > 0) {
				health = Math.Min(health + healAmount, Stats.MaxHealth);
			}
		}
	}

	private void HandleInput(InputCommands input, float deltaTime, TileMap map = null) {
		// Attack input
		if(input.AttackHeld) {  // Use InputCommands!
			Attack();
		}
		if(input.DodgePressed) {
			Dodge();
		}

		// Get movement from InputCommands (already normalized!)
		Vector2 movement = input.Movement;

		// Update last move direction if moving
		if(movement.Length() > 0) {
			_lastMoveDirection = movement;
		}

		// Calculate velocity (use stats speed)
		if(_isDodging) {
			// During dodge, move at high speed in dodge direction
			Velocity = _dodgeDirection * DODGE_SPEED;
		} else {
			// Normal movement
			Velocity = movement * Stats.Speed * _speedMultiplier;
		}

		// Apply movement with collision detection
		Vector2 newPosition = Position + Velocity * deltaTime;

		// If we have a map, check collision
		if(map != null) {
			// Try horizontal movement
			Rectangle horizontalBounds = new Rectangle(
				(int)newPosition.X,
				(int)Position.Y,
				Width,
				Height
			);

			if(!map.checkCollision(horizontalBounds)) {
				Position = new Vector2(newPosition.X, Position.Y);
			}

			// Try vertical movement
			Rectangle verticalBounds = new Rectangle(
				(int)Position.X,
				(int)newPosition.Y,
				Width,
				Height
			);

			if(!map.checkCollision(verticalBounds)) {
				Position = new Vector2(Position.X, newPosition.Y);
			}
		} else {
			Position = newPosition;
		}
	}

	public void CollectPickup(Pickup pickup) {
		if(pickup.HealthRestore > 0) {
			health = Math.Min(health + pickup.HealthRestore, Stats.MaxHealth);
		}

		if(pickup.CoinValue > 0) {
			Coins += pickup.CoinValue;
		}

		pickup.Collect();
	}

	public bool GainXP(int amount) {
		XP += amount;

		if(XP >= XPToNextLevel) {
			LevelUp();
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
		this.health = this.Stats.MaxHealth;
		this.XP = 0;
		this.Level = 1;
		this.Coins = 0;

		// Clear inventory
		this.Inventory = new Inventory();
	}

	public override void Draw(SpriteBatch spriteBatch) {
		base.Draw(spriteBatch);
		Color tint = Color.White;
		if(_isDodging) {
			// Make player slightly transparent during dodge
			tint = Color.White * 0.7f;
		} else if(IsInvincible && (_invincibilityTimer * 10) % 1 > 0.5f) {
			// Flash when invincible (but not dodging)
			tint = Color.White * 0.5f;
		}
	}
}