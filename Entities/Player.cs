using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Result;
using EldmeresTale.Entities.Factories;
using EldmeresTale.Events;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
using EldmeresTale.Systems.VFX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Entities;

public class Player {

	private const int BASE_XP_TO_LEVEL_UP = 100;
	private const float XP_MULTIPLIER_REQUIREMENT_GROWTH_FACTOR = 1.5f;

	// =================== CLEAN ========================

	public Entity Entity { get; set; }

	public Vector2 Position {
		get => Entity.Get<Position>().Value;
		set => Entity.Get<Position>().Value = value;
	}

	public Vector2 Speed {
		get => Entity.Get<Velocity>().Value;
		set => Entity.Get<Velocity>().Value = value;
	}

	public Vector2 Direction {
		get => Entity.Has<Velocity>() ? Entity.Get<Velocity>().Direction : Vector2.UnitX;
	}

	public int Health {
		get {
			return Entity.Has<Health>() ? Entity.Get<Health>().Current : 0;
		}
		set => Entity.Get<Health>().Current = value;
	}
	public Rectangle Bounds => Entity.Has<Collider>() ? Entity.Get<Collider>().GetBounds(Position) : new Rectangle(Position.ToPoint(), new Point(0, 0));
	public Inventory Inventory { get; private set; }
	public int MaxHealth => Stats.MaxHealth;
	public bool IsDying { get => Entity.Has<JustDied>(); }

	public bool IsDead { get; private set; }

	// =================== DIRTY ========================

	// Size
	public int Width { get; set; }
	public int Height { get; set; }


	// Stats system
	public PlayerStats Stats { get; set; }

	private GameEventBus _eventBus;

	// dodging 
	private bool _isDodging = false;
	private float _dodgeTimer = 0f;
	private float _dodgeCooldown = 0f;

	private const float DODGE_DURATION = 0.2f;         // How long the dash lasts
	private const float DODGE_COOLDOWN = 1.0f;         // Cooldown between dodges
	public bool CanDodge => !_isDodging && !_isAttacking && _dodgeCooldown <= 0f;

	// dodge fx
	private struct TrailFrame {
		public Vector2 Position;
		public float Alpha;
		public Rectangle? SourceRect;  // For animated sprites
	}

	private readonly List<TrailFrame> _dodgeTrail = [];
	private const int MAX_TRAIL_FRAMES = 5;
	private const float TRAIL_SPAWN_INTERVAL = 0.03f;  // Spawn trail every 0.03 seconds
	private float _trailSpawnTimer = 0f;


	// Attack properties
	private float _attackCooldown = 0f;
	private bool _isAttacking = false;
	private readonly float _attackDuration = 0.2f;
	private float _attackTimer = 0f;

	// Attack effect
	private readonly AttackEffect _attackEffect;

	// Health regeneration timer
	private float _regenTimer = 0f;
	private const float REGEN_TICK_RATE = 1f; // Regen applies every second

	public bool CanAttack => _attackCooldown <= 0 && !_isAttacking;

	// Player progression
	public int Coins { get; set; } = 0;
	public int Level { get; set; }
	public int XP { get; set; } = 0;
	public int XPToNextLevel => CalculateXPToNextLevel(Level);

	public event Action OnPlayerDeath;
	public event Action<Vector2> OnDodge;

	public event Action<Player> OnAttacked;

	// Constructor for animated sprite
	public Player(Texture2D defaultTexture, int width = 32, int height = 32) {
		Stats = new PlayerStats();
		Width = width;
		Height = height;
		Inventory = new Inventory(maxSize: 50);
		Level = 1;
		XP = 0;
		_dodgeTrail = [];
		_attackEffect = new AttackEffect(defaultTexture);
	}

	public void SetEventBus(GameEventBus eventBus) {
		_eventBus = eventBus;
	}

	public void Update(GameTime gameTime, InputCommands input) {
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
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
					Sprite sprite = Entity.Get<Sprite>();
					// Add new trail frame
					TrailFrame frame = new TrailFrame {
						Position = Position,
						Alpha = 0.6f,
						SourceRect = sprite.SourceRect
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
			if (_isAttacking) {
				_attackTimer -= deltaTime;
				if (_attackTimer <= 0) {
					_isAttacking = false;
				}
			}

			// Apply health regeneration
			ApplyHealthRegen(deltaTime);

			// Update attack effect
			_attackEffect?.Update(gameTime);

			HandleInput(input, deltaTime);
		} else {
			IsDead = true;
		}
	}

	private void ApplyHealthRegen(float deltaTime) {
		if (Stats.HealthRegen <= 0 || Health >= Stats.MaxHealth) {
			return;
		}

		_regenTimer += deltaTime;

		if (_regenTimer >= REGEN_TICK_RATE) {
			_regenTimer -= REGEN_TICK_RATE;
			Health = Math.Min(Health + (int)Stats.HealthRegen, Stats.MaxHealth);
		}
	}

	public void Attack() {
		if (CanAttack) {
			_isAttacking = true;
			_attackTimer = _attackDuration;
			_attackCooldown = Stats.AttackCooldownDuration;
			CombatStats stats = Entity.Get<CombatStats>();
			Faction faction = Entity.Get<Faction>();
			Position position = Entity.Get<Position>();
			Entity.Set(new Attacking {
				Angle = stats.AttackAngle,
				AttackDamage = stats.AttackDamage,
				AttackerFaction = faction.Name,
				AttackRange = stats.AttackRange,
				CritChance = stats.CritChance,
				CritMultiplier = stats.CritMultiplier,
				Direction = Direction,
				Origin = position.Value + new Vector2(0, -Height / 2f)
			});
			_eventBus?.Publish(new PlayerAttackEvent {
				Player = this
			});
		}
	}

	public void TriggerAttackEffect() {
		CombatStats stats = Entity.Get<CombatStats>();
		_attackEffect?.Trigger(() => Position + new Vector2(0, -Height / 2f), Direction, stats.AttackRange, stats.AttackAngle);
	}


	public void Dodge() {
		if (!CanDodge) {
			return;
		}
		ref Velocity velocity = ref Entity.Get<Velocity>();
		velocity.Impulse += Direction * 1000;
		Entity.Set(new PlaySound("dodge_whoosh", Entity.Get<Position>().Value));
		ref Health health = ref Entity.Get<Health>();
		health.InvincibilityTimer = 1;
		_isDodging = true;
		_dodgeTimer = DODGE_DURATION;
		_dodgeCooldown = DODGE_COOLDOWN;
		_trailSpawnTimer = 0f;
		_dodgeTrail.Clear();

		OnDodge?.Invoke(Direction);
		_eventBus?.Publish(new PlayerDodgeEvent {
			DodgeDirection = Direction,
			Position = Position
		});

		System.Diagnostics.Debug.WriteLine($"[DODGE] Rolling! Direction: {Direction}");
	}

	private void HandleInput(InputCommands input, float deltaTime) {
		if (IsDead) {
			return;
		}
		// Attack input
		if (input.AttackHeld) {  // Use InputCommands!
			Attack();
		}
		if (input.DodgePressed) {
			Dodge();
		}

		ref Velocity vel = ref Entity.Get<Velocity>();
		vel.Value = input.Movement * Stats.Speed;
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

		// Apply stat bonuses through the stats system
		Stats.ApplyLevelUpBonus();

		// Fully heal on level up
		Health = Stats.MaxHealth;
	}

	public static int CalculateXPToNextLevel(int currentLevel) {
		return (int)(BASE_XP_TO_LEVEL_UP * Math.Pow(XP_MULTIPLIER_REQUIREMENT_GROWTH_FACTOR, currentLevel - 1));
	}

	public void CollectPickup(PickupType pickupType, int pickupValue) {
		if (pickupType == PickupType.Health) {
			Health = Math.Min(Health + pickupValue, Stats.MaxHealth);
		}
		if (pickupType == PickupType.Coin || pickupType == PickupType.BigCoin) {
			Coins += pickupValue;
		}
		if (pickupType == PickupType.XP) {
			GainXP(pickupValue);
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (_isDodging && _dodgeTrail.Count > 0) {
			DrawDodgeTrail(spriteBatch);
		}
		// Attack effect BEFORE player if attacking up
		_attackEffect?.Draw(spriteBatch);
	}

	private void DrawDodgeTrail(SpriteBatch spriteBatch) {
		Sprite sprite = Entity.Get<Sprite>();
		foreach (TrailFrame frame in _dodgeTrail) {
			Color trailTint = Color.White * frame.Alpha;
			if (frame.SourceRect.HasValue) {
				// Draw sprite trail
				Vector2 spritePosition = new Vector2(
					frame.Position.X + ((Width - frame.SourceRect.Value.Width) / 2f) - (Width / 2),
					frame.Position.Y + ((Height - frame.SourceRect.Value.Height) / 2f) - Height
				);
				spriteBatch.Draw(sprite.Texture, spritePosition, frame.SourceRect, trailTint);
			}
		}
	}


	public void GiveRewards(QuestReward rewards) {
		if (rewards.Xp > 0) {
			GainXP(rewards.Xp);
			System.Diagnostics.Debug.WriteLine($"Rewarded {rewards.Xp} XP");
		}

		if (rewards.Gold > 0) {
			Coins += rewards.Gold;
			System.Diagnostics.Debug.WriteLine($"Rewarded {rewards.Gold} gold");
		}

		foreach (string itemId in rewards.Items) {
			Inventory.AddItem(EquipmentFactory.CreateFromId(itemId));
			System.Diagnostics.Debug.WriteLine($"Rewarded item: {itemId}");
		}
	}
}