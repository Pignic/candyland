using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class AISystem : AEntitySetSystem<float> {
	private readonly Player _player;
	private readonly Random _random;

	public AISystem(World world, Player player)
		: base(world.GetEntities()
			.With<Position>()
			.With<Velocity>()
			.With<AIBehavior>()
			.With((in Health h) => !h.IsDead)
			.AsSet()) {
		_player = player;
		_random = new Random();
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Position pos = ref entity.Get<Position>();
		ref Velocity vel = ref entity.Get<Velocity>();
		ref AIBehavior ai = ref entity.Get<AIBehavior>();
		Health health = entity.Get<Health>();
		EnemyType enemyType = entity.Get<EnemyType>();

		// Update timers
		ai.StateTimer += deltaTime;
		if (ai.AttackCooldown > 0) {
			ai.AttackCooldown -= deltaTime;
		}

		// Check if player is in detection range
		Vector2 playerPos = _player.Position + new Vector2(_player.Width / 2, _player.Height / 2);
		Vector2 enemyCenter = pos.Value + new Vector2(16, 16); // Assume 32x32 enemy
		float distanceToPlayer = Vector2.Distance(enemyCenter, playerPos);

		// Update behavior based on type
		switch (ai.BehaviorType) {
			case AIBehaviorType.Idle:
				UpdateIdleBehavior(ref ai, ref vel);
				break;

			case AIBehaviorType.Patrol:
				UpdatePatrolBehavior(ref ai, ref vel, ref pos, enemyType.PatrolSpeed, deltaTime);
				break;

			case AIBehaviorType.Wander:
				UpdateWanderBehavior(ref ai, ref vel, ref pos, enemyType.PatrolSpeed, deltaTime);
				break;

			case AIBehaviorType.Chase:
				UpdateChaseBehavior(ref ai, ref vel, pos.Value, playerPos, distanceToPlayer, enemyType.PatrolSpeed);
				break;
		}
	}

	private void UpdateIdleBehavior(ref AIBehavior ai, ref Velocity vel) {
		ai.CurrentState = AIState.Idle;
		vel.Value = Vector2.Zero;
	}

	private void UpdatePatrolBehavior(ref AIBehavior ai, ref Velocity vel, ref Position pos, float speed, float deltaTime) {
		if (ai.PatrolPoints == null || ai.PatrolPoints.Length == 0) {
			UpdateIdleBehavior(ref ai, ref vel);
			return;
		}

		ai.CurrentState = AIState.Patrol;

		Vector2 targetPoint = ai.PatrolPoints[ai.CurrentPatrolIndex];
		Vector2 direction = targetPoint - pos.Value;
		float distance = direction.Length();

		if (distance < 10f) {
			// Reached patrol point, move to next
			ai.CurrentPatrolIndex = (ai.CurrentPatrolIndex + 1) % ai.PatrolPoints.Length;
			vel.Value = Vector2.Zero;
		} else {
			// Move toward patrol point
			direction.Normalize();
			vel.Value = direction * speed;
		}
	}

	private void UpdateWanderBehavior(ref AIBehavior ai, ref Velocity vel, ref Position pos, float speed, float deltaTime) {
		ai.CurrentState = AIState.Wander;

		ai.WanderTimer -= deltaTime;

		if (ai.WanderTimer <= 0) {
			// Pick new wander target
			float angle = (float)(_random.NextDouble() * Math.PI * 2);
			float distance = (float)_random.NextDouble() * ai.WanderRadius;
			ai.WanderTarget = ai.WanderCenter + new Vector2(
				MathF.Cos(angle) * distance,
				MathF.Sin(angle) * distance
			);
			ai.WanderTimer = ai.WanderInterval;
		}

		// Move toward wander target
		Vector2 direction = ai.WanderTarget - pos.Value;
		float dist = direction.Length();

		if (dist < 10f) {
			vel.Value = Vector2.Zero;
		} else {
			direction.Normalize();
			vel.Value = direction * (speed * 0.5f); // Wander slower than chase
		}
	}

	private void UpdateChaseBehavior(ref AIBehavior ai, ref Velocity vel, Vector2 enemyPos, Vector2 playerPos, float distanceToPlayer, float speed) {
		// Check if player is in detection range
		if (distanceToPlayer < ai.DetectionRange) {
			ai.CurrentState = AIState.Chase;
			ai.HasTarget = true;

			// Check if in attack range
			if (distanceToPlayer < ai.AttackRange) {
				ai.CurrentState = AIState.Attack;
				vel.Value = Vector2.Zero;
				// Attack handled by CombatSystem
			} else {
				// Chase player
				Vector2 direction = playerPos - enemyPos;
				direction.Normalize();
				vel.Value = direction * ai.ChaseSpeed;
			}
		} else if (distanceToPlayer > ai.ChaseGiveUpDistance && ai.HasTarget) {
			// Lost player
			ai.HasTarget = false;
			ai.CurrentState = AIState.Idle;
			vel.Value = Vector2.Zero;
		} else if (!ai.HasTarget) {
			// No target, idle
			ai.CurrentState = AIState.Idle;
			vel.Value = Vector2.Zero;
		}
	}
}