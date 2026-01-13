using DefaultEcs;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class CombatSystem : GameSystem {
	private readonly Player _player;
	private List<Entity> _enemies;
	private readonly List<Entity> _props;
	private float _pauseTimer = 0f;
	private readonly GameEventBus _eventBus;

	public bool IsPaused => _pauseTimer > 0f;

	public CombatSystem(Player player, GameEventBus eventBus) {
		Enabled = true;
		Visible = false;
		_player = player;
		_enemies = [];
		_props = [];
		_eventBus = eventBus;
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[COMBAT SYSTEM] Initialized");
	}

	public void Pause(float duration) {
		_pauseTimer = duration;
	}

	public override void Update(GameTime gameTime) {
		if (!Enabled) {
			return;
		}

		if (_pauseTimer > 0f) {
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_pauseTimer -= deltaTime;
			return;
		}

		// Player attacking enemies
		ProcessPlayerAttackingEnemies();

		// Player attacking props
		ProcessPlayerAttackingProps();

		// Enemies attacking player
		ProcessEnemiesAttackingPlayer();
	}

	private void ProcessPlayerAttackingEnemies() {
		if (_player.AttackBounds == Rectangle.Empty) {
			return;
		}

		//foreach (Enemy enemy in _enemies) {
		//	// Check if enemy can be hit
		//	if (!enemy.IsAlive) {
		//		continue;
		//	}

		//	if (_player.HasHitEntity(enemy)) {
		//		continue; // Already hit this attack
		//	}

		//	if (!_player.AttackBounds.Intersects(enemy.Bounds)) {
		//		continue;
		//	}

		//	// Calculate damage
		//	(int damage, bool wasCrit) = _player.CalculateDamage();

		//	// Store if enemy was alive before damage
		//	bool wasAlive = enemy.IsAlive;

		//	// Apply damage
		//	Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);
		//	enemy.TakeDamage(damage, playerCenter);

		//	// Mark enemy as hit so we don't hit it multiple times
		//	_player.MarkEntityAsHit(enemy);

		//	// Apply lifesteal
		//	_player.OnDamageDealt(damage);

		//	// Fire hit event
		//	Vector2 damagePos = enemy.Position + new Vector2(enemy.Width / 2f, 0);
		//	_eventBus.Publish(new EnemyHitEvent {
		//		Enemy = enemy,
		//		Damage = damage,
		//		WasCritical = wasCrit,
		//		DamagePosition = damagePos,
		//		Position = damagePos
		//	});

		//	// Check if enemy was killed
		//	if (wasAlive && !enemy.IsAlive) {
		//		_eventBus.Publish(new EnemyKilledEvent {
		//			Enemy = enemy,
		//			DeathPosition = damagePos,
		//			Position = damagePos
		//		});
		//	}
		//}
	}

	private void ProcessPlayerAttackingProps() {
		if (_player.AttackBounds == Rectangle.Empty) {
			return;
		}

		//foreach (Prop prop in _props) {
		//	// Check if prop can be hit
		//	if (prop.Type != PropType.Breakable) {
		//		continue;
		//	}

		//	if (!prop.IsActive) {
		//		continue;
		//	}

		//	if (!_player.AttackBounds.Intersects(prop.Bounds)) {
		//		continue;
		//	}

		//	// Calculate damage
		//	(int damage, bool wasCrit) = _player.CalculateDamage();

		//	// Store if prop was active before damage
		//	bool wasActive = prop.IsActive;

		//	// Apply damage
		//	prop.TakeDamage(damage);

		//	// Fire hit event
		//	Vector2 damagePos = prop.Position + new Vector2(prop.Width / 2f, 0);
		//	_eventBus.Publish(new PropHitEvent {
		//		Prop = prop,
		//		Damage = damage,
		//		WasCritical = wasCrit,
		//		DamagePosition = damagePos,
		//		Position = damagePos
		//	});

		//	// Check if prop was destroyed
		//	if (wasActive && !prop.IsActive) {
		//		_eventBus.Publish(new PropDestroyedEvent {
		//			Prop = prop,
		//			DestructionPosition = damagePos,
		//			Position = damagePos
		//		});
		//	}
		//}
	}

	private void ProcessEnemiesAttackingPlayer() {
		//foreach (Enemy enemy in _enemies) {
		//	// Check if enemy can attack
		//	if (!enemy.IsAlive) {
		//		continue;
		//	}

		//	if (!enemy.Bounds.Intersects(_player.Bounds)) {
		//		continue;
		//	}

		//	if (_player.IsInvincible) {
		//		continue;
		//	}

		//	// Apply damage to player
		//	Vector2 enemyCenter = enemy.Position + new Vector2(enemy.Width / 2f, enemy.Height / 2f);
		//	_player.TakeDamage(enemy.AttackDamage, enemyCenter);

		//	// Fire hit event
		//	Vector2 damagePos = _player.Position + new Vector2(_player.Width / 2f, 0);
		//	_eventBus.Publish(new PlayerHitEvent {
		//		AttackingEnemy = enemy,
		//		Damage = enemy.AttackDamage,
		//		DamagePosition = damagePos,
		//		Position = damagePos
		//	});
		//}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// Combat system doesn't draw anything
	}

	public override void OnRoomChanged(Room newRoom) {
		// Update enemy and prop references
		_enemies = newRoom.Enemies;
		//_props = newRoom.Props;

		//System.Diagnostics.Debug.WriteLine($"[COMBAT SYSTEM] Room changed - tracking {_enemies.Count} enemies, {_props.Count} props");
	}

	public override void Dispose() {
		System.Diagnostics.Debug.WriteLine("[COMBAT SYSTEM] Disposed");
	}
}