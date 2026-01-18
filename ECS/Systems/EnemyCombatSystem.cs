using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public sealed class EnemyCombatSystem : AEntitySetSystem<float> {
	private readonly Entity _player;

	public EnemyCombatSystem(World world, Player player)
		: base(world.GetEntities()
			.With<RoomId>()
			.With<CombatStats>()
			.With<Position>()
			.With<Collider>()
			.With<EnemyType>()
			.With<AIBehavior>()
			.With((in Health h) => !h.IsDead)
			.AsSet()) {
		_player = player.Entity;
	}

	protected override void Update(float deltaTime, in Entity entity) {
		RoomId roomId = entity.Get<RoomId>();
		Position position = entity.Get<Position>();
		Collider collider = entity.Get<Collider>();
		ref AIBehavior ai = ref entity.Get<AIBehavior>();

		// Only attack if in attack state and cooldown ready
		if (ai.CurrentState != AIState.Attack || ai.AttackCooldown > 0) {
			return;
		}

		// Check collision with player
		Rectangle enemyBounds = collider.GetBounds(position);
		Position playerPosition = _player.Get<Position>();
		Rectangle playerBounds = _player.Get<Collider>().GetBounds(playerPosition);

		if (enemyBounds.Intersects(playerBounds)) {
			CombatStats combatStats = entity.Get<CombatStats>();
			Faction faction = entity.Get<Faction>();
			entity.Set(new Attacking {
				Angle = combatStats.AttackAngle,
				AttackDamage = combatStats.AttackDamage,
				AttackerFaction = faction.Name,
				AttackRange = combatStats.AttackRange,
				CritChance = combatStats.CritChance,
				CritMultiplier = combatStats.CritMultiplier,
				Direction = playerPosition.Value - position.Value,
				Origin = position.Value + collider.Offset,
				RoomId = roomId.Name
			});

			// Set attack cooldown
			ai.AttackCooldown = combatStats.AttackCooldown;
		}
	}
}