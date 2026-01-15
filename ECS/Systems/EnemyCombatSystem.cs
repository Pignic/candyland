using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Factories;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public sealed class EnemyCombatSystem : AEntitySetSystem<float> {
	private readonly Player _player;
	private readonly ParticleEmitter _particleEmitter;

	public EnemyCombatSystem(World world, Player player, ParticleEmitter particleEmitter)
		: base(world.GetEntities()
			.With<Position>()
			.With<Collider>()
			.With<EnemyType>()
			.With<AIBehavior>()
			.With((in Health h) => !h.IsDead)
			.AsSet()) {
		_player = player;
		_particleEmitter = particleEmitter;
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Position pos = entity.Get<Position>();
		Collider collider = entity.Get<Collider>();
		EnemyType enemyType = entity.Get<EnemyType>();
		ref AIBehavior ai = ref entity.Get<AIBehavior>();
		Health health = entity.Get<Health>();

		// Only attack if in attack state and cooldown ready
		if (ai.CurrentState != AIState.Attack || ai.AttackCooldown > 0) {
			return;
		}

		// Check collision with player
		Rectangle enemyBounds = collider.GetBounds(pos);
		Rectangle playerBounds = _player.Bounds;

		if (enemyBounds.Intersects(playerBounds)) {
			// Damage player
			Vector2 knockbackDirection = _player.Position - pos.Value;
			knockbackDirection.Normalize();

			//_player.TakeDamage(enemyType.Damage, pos.Value);

			// Spawn blood particles
			Vector2 hitPosition = _player.Position + new Vector2(_player.Width / 2, _player.Height / 2);
			_particleEmitter.SpawnBloodSplatter(hitPosition, knockbackDirection, 10);

			// Set attack cooldown
			//ai.AttackCooldown = enemyType.AttackCooldown;
		}
	}
}