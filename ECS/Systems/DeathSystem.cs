using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Result;
using EldmeresTale.ECS.Factories;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class DeathSystem : AEntitySetSystem<float> {
	private readonly ParticleEmitter _particleEmitter;
	private readonly PickupFactory _pickupFactory;
	private readonly Random _random;

	public DeathSystem(World world, ParticleEmitter particleEmitter, PickupFactory pickupFactory)
		: base(world.GetEntities()
			.With<Position>()
			.With<Sprite>()
			.With((in Health h) => !h.IsDead)
			.AsSet()) {
		_particleEmitter = particleEmitter;
		_pickupFactory = pickupFactory;
		_random = new Random();
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

		// Spawn loot
		if (entity.Has<Lootable>()) {
			Lootable lootable = entity.Get<Lootable>();
			SpawnLoot(roomId.Name, deathPosition, lootable);
		}

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

	private void SpawnLoot(string roomId, Vector2 position, Lootable loot) {
		// Always spawn XP
		_pickupFactory.CreatePickup(PickupType.XP, position + new Vector2(-10, 0), roomId, loot.XPAmount);

		// Spawn coins
		if (_random.NextDouble() < loot.CoinDropChance) {
			int coinAmount = _random.Next(loot.CoinMin, loot.CoinMax + 1);
			_pickupFactory.CreatePickup(PickupType.Coin, position, roomId, coinAmount);
		}

		// Spawn health
		if (_random.NextDouble() < loot.HealthDropChance) {
			_pickupFactory.CreatePickup(PickupType.Health, position + new Vector2(10, 0), roomId, loot.HealthAmount);
		}
	}
}