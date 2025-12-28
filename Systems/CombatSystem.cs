using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class CombatSystem : GameSystem {
	private readonly Player _player;
	private List<Enemy> _enemies;
	private List<Prop> _props;

	// Combat events that other systems can subscribe to
	public event Action<Enemy, int, bool, Vector2> OnEnemyHit;
	public event Action<Enemy, Vector2> OnEnemyKilled;
	public event Action<Prop, int, bool, Vector2> OnPropHit;
	public event Action<Prop, Vector2> OnPropDestroyed;
	public event Action<Enemy, int, Vector2> OnPlayerHit;

	public CombatSystem(Player player) {
		_player = player;
		Enabled = true;
		_enemies = new List<Enemy>();
		_props = new List<Prop>();
		Visible = false; // Combat system doesn't draw anything
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[COMBAT SYSTEM] Initialized");
	}

	public override void Update(GameTime gameTime) {
		if(!Enabled) return;

		// Player attacking enemies
		ProcessPlayerAttackingEnemies();

		// Player attacking props
		ProcessPlayerAttackingProps();

		// Enemies attacking player
		ProcessEnemiesAttackingPlayer();
	}

	private void ProcessPlayerAttackingEnemies() {
		if(_player.AttackBounds == Rectangle.Empty) return;

		foreach(var enemy in _enemies) {
			// Check if enemy can be hit
			if(!enemy.IsAlive) continue;
			if(_player.HasHitEntity(enemy)) continue; // Already hit this attack
			if(!_player.AttackBounds.Intersects(enemy.Bounds)) continue;

			// Calculate damage
			var (damage, wasCrit) = _player.CalculateDamage();

			// Store if enemy was alive before damage
			bool wasAlive = enemy.IsAlive;

			// Apply damage
			Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);
			enemy.TakeDamage(damage, playerCenter);

			// Mark enemy as hit so we don't hit it multiple times
			_player.MarkEntityAsHit(enemy);

			// Apply lifesteal
			_player.OnDamageDealt(damage);

			// Fire hit event
			Vector2 damagePos = enemy.Position + new Vector2(enemy.Width / 2f, 0);
			OnEnemyHit?.Invoke(enemy, damage, wasCrit, damagePos);

			// Check if enemy was killed
			if(wasAlive && !enemy.IsAlive) {
				OnEnemyKilled?.Invoke(enemy, damagePos);
			}
		}
	}

	private void ProcessPlayerAttackingProps() {
		if(_player.AttackBounds == Rectangle.Empty) return;

		foreach(var prop in _props) {
			// Check if prop can be hit
			if(prop.type != PropType.Breakable) continue;
			if(!prop.isActive) continue;
			if(!_player.AttackBounds.Intersects(prop.Bounds)) continue;

			// Calculate damage
			var (damage, wasCrit) = _player.CalculateDamage();

			// Store if prop was active before damage
			bool wasActive = prop.isActive;

			// Apply damage
			prop.TakeDamage(damage);

			// Fire hit event
			Vector2 damagePos = prop.Position + new Vector2(prop.Width / 2f, 0);
			OnPropHit?.Invoke(prop, damage, wasCrit, damagePos);

			// Check if prop was destroyed
			if(wasActive && !prop.isActive) {
				OnPropDestroyed?.Invoke(prop, damagePos);
			}
		}
	}

	private void ProcessEnemiesAttackingPlayer() {
		foreach(var enemy in _enemies) {
			// Check if enemy can attack
			if(!enemy.IsAlive) continue;
			if(!enemy.Bounds.Intersects(_player.Bounds)) continue;
			if(_player.IsInvincible) continue;

			// Apply damage to player
			Vector2 enemyCenter = enemy.Position + new Vector2(enemy.Width / 2f, enemy.Height / 2f);
			_player.TakeDamage(enemy.AttackDamage, enemyCenter);

			// Fire hit event
			Vector2 damagePos = _player.Position + new Vector2(_player.Width / 2f, 0);
			OnPlayerHit?.Invoke(enemy, enemy.AttackDamage, damagePos);
		}
	}

	public void SetEnemies(List<Enemy> enemies) {
		_enemies = enemies;
	}

	public void SetProps(List<Prop> props) {
		_props = props;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// Combat system doesn't draw anything
	}

	public override void Dispose() {
		// Clear event subscriptions
		OnEnemyHit = null;
		OnEnemyKilled = null;
		OnPropHit = null;
		OnPropDestroyed = null;
		OnPlayerHit = null;

		System.Diagnostics.Debug.WriteLine("[COMBAT SYSTEM] Disposed");
	}
}