using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Factories;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class EnemyDeathSystem : AEntitySetSystem<float> {
	private readonly ParticleEmitter _particleEmitter;
	private readonly PickupFactory _pickupFactory;
	private readonly Random _random;

	public EnemyDeathSystem(World world, ParticleEmitter particleEmitter, PickupFactory pickupFactory)
		: base(world.GetEntities()
			.With<EnemyType>()
			.With<Lootable>()
			.With<Position>()
			.With<Sprite>()
			.With((in Health h) => !h.IsDead)
			.AsSet()) {
		_particleEmitter = particleEmitter;
		_pickupFactory = pickupFactory;
		_random = new Random();
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Health health = entity.Get<Health>();

		// Check if enemy just died
		if (health.IsDead) {
			HandleDeath(entity);
		}
	}

	private void HandleDeath(Entity entity) {
		Position pos = entity.Get<Position>();
		EnemyType enemyType = entity.Get<EnemyType>();
		Lootable lootable = entity.Get<Lootable>();
		Sprite sprite = entity.Get<Sprite>();
		Collider collider = entity.Get<Collider>();

		Vector2 deathPosition = pos.Value + new Vector2(collider.Width / 2, collider.Height / 2);

		// Spawn death particles
		_particleEmitter.SpawnBurst(
			deathPosition,
			Color.Red,
			count: 20,
			speed: 150f,
			size: 4f
		);

		_particleEmitter.SpawnBloodSplatter(
			deathPosition,
			new Vector2(0, -1),
			25
		);

		// Spawn loot
		SpawnLoot(deathPosition, lootable);

		// Start death animation
		entity.Set(new DeathAnimation(0.8f) {
			InitialColor = sprite.Tint
		});

		// Remove AI and velocity so it stops moving
		entity.Remove<AIBehavior>();
		entity.Remove<Velocity>();

		// Remove collider so player can walk through corpse
		entity.Remove<Collider>();
	}

	private void SpawnLoot(Vector2 position, Lootable loot) {
		// Always spawn XP
		_pickupFactory.CreateXPPickup(position + new Vector2(-10, 0), loot.XPAmount);

		// Spawn coins
		if (_random.NextDouble() < loot.CoinDropChance) {
			int coinAmount = _random.Next(loot.CoinMin, loot.CoinMax + 1);
			_pickupFactory.CreateCoinPickup(position, coinAmount);
		}

		// Spawn health
		if (_random.NextDouble() < loot.HealthDropChance) {
			_pickupFactory.CreateHealthPickup(position + new Vector2(10, 0), loot.HealthAmount);
		}
	}
}