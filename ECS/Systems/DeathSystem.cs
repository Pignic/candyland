using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Result;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.ECS.Factories;
using EldmeresTale.Events;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public sealed class DeathSystem : AEntitySetSystem<float> {
	private readonly ParticleEmitter _particleEmitter;

	public DeathSystem(World world, ParticleEmitter particleEmitter)
		: base(world.GetEntities()
			.With<Position>()
			.With<Sprite>()
			.With((in Health h) => !h.IsDead)
			.AsSet()) {
		_particleEmitter = particleEmitter;
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Health health = ref entity.Get<Health>();
		if (health.Current <= 0) {
			health.IsDead = true;
		}
		// Check if enemy just died
		if (health.IsDead) {
			HandleDeath(entity);
		}
	}

	private void HandleDeath(Entity entity) {
		Position pos = entity.Get<Position>();
		Sprite sprite = entity.Get<Sprite>();
		RoomId roomId = entity.Get<RoomId>();

		Vector2 deathPosition = pos.Value;
		if (entity.Has<Collider>()) {
			deathPosition += entity.Get<Collider>().Offset;
		}
		Faction faction = entity.Get<Faction>();

		if (faction.Name == FactionName.Enemy) {
			// Enemy died - publish event
			string enemyType = entity.Has<EnemyType>() ? entity.Get<EnemyType>().TypeName : "unknown";

			entity.Set(new ECSEvent(new EnemyDeathEvent {
				EnemyType = enemyType,
				DeathPosition = deathPosition
			}));
		}

		entity.Set(new JustDied(deathPosition));
		entity.Set(new PlaySound("monster_growl_mid", deathPosition));

		// Spawn death particles
		_particleEmitter.SpawnBurst(roomId.Name,
			deathPosition,
			Color.Red,
			count: 20,
			speed: 150f,
			size: 4f
		);

		_particleEmitter.SpawnBloodSplatter(roomId.Name,
			deathPosition,
			new Vector2(0, -1),
			25
		);

		// Start death animation
		entity.Set(new DeathAnimation(0.8f) {
			InitialColor = sprite.Tint
		});

		// Remove AI and velocity so it stops moving
		entity.Remove<AIBehavior>();
		entity.Remove<Velocity>();

		// Remove collider so player can walk through corpse
		entity.Remove<Collider>();
		entity.Remove<Health>();
	}
}