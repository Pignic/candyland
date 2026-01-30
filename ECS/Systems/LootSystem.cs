using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Result;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.ECS.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EldmeresTale.ECS.Systems;

public sealed class LootSystem : AEntitySetSystem<float> {

	private readonly PickupFactory _pickupFactory;
	private readonly Random _random;

	public LootSystem(World world, PickupFactory pickupFactory)
		: base(world.GetEntities()
			.With<Lootable>()
			.WhenAdded<JustDied>()
			.With<RoomId>()
			.AsSet()) {
		_pickupFactory = pickupFactory;
		_random = new Random();
	}

	protected override void Update(float state, in Entity entity) {
		Lootable loot = entity.Get<Lootable>();
		JustDied justDied = entity.Get<JustDied>();
		RoomId roomId = entity.Get<RoomId>();

		if (loot.XPAmount > 0) {
			_pickupFactory.CreatePickup(PickupType.XP, justDied.Location + new Vector2(-10, 0), roomId.Name, loot.XPAmount);
		}

		// Spawn coins
		int coinAmount = _random.Next(loot.CoinMin, loot.CoinMax + 1);
		Vector2 impulse = Vector2.Normalize(new Vector2(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f)) * 300f * _random.NextSingle();
		float zImpulse = _random.NextSingle() * 30;
		while (coinAmount > 0) {
			if (coinAmount >= 5 && _random.NextDouble() > 0.3) {
				coinAmount -= 5;
				_pickupFactory.CreatePickup(PickupType.BigCoin, justDied.Location, roomId.Name, 5, impulse, zImpulse);
			} else {
				coinAmount--;
				_pickupFactory.CreatePickup(PickupType.Coin, justDied.Location, roomId.Name, 1, impulse, zImpulse);
			}
			impulse = Vector2.Normalize(new Vector2(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f)) * 300f * _random.NextSingle();
			zImpulse = _random.NextSingle() * 30;
		}
		// Spawn health
		if (_random.NextDouble() < loot.HealthDropChance) {
			_pickupFactory.CreatePickup(PickupType.Health, justDied.Location, roomId.Name, loot.HealthAmount, impulse, zImpulse);
			impulse = Vector2.Normalize(new Vector2(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f)) * 300f * _random.NextSingle();
			zImpulse = _random.NextSingle() * 30;
		}

		// Drop materials
		foreach (KeyValuePair<string, float> kv in loot.LootTable) {
			if (_random.NextDouble() <= kv.Value) {
				_pickupFactory.CreatePickup(PickupType.Material, justDied.Location, roomId.Name, 1, impulse, zImpulse, kv.Key);
				impulse = Vector2.Normalize(new Vector2(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f)) * 300f * _random.NextSingle();
				zImpulse = _random.NextSingle() * 30;
				System.Diagnostics.Debug.WriteLine($"[LOOT] Droping {kv.Key} in {roomId.Name} at {justDied.Location.ToString()}");
			}
		}

		base.Update(state, entity);
	}
}