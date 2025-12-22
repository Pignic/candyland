using Candyland.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Candyland.Systems;

public class LootSystem : GameSystem {
	private List<Pickup> _pickups;
	private Random _random;

	public void SpawnLoot(Enemy enemy) {
		Vector2 dropPos = enemy.Position + new Vector2(enemy.Width / 2f - 8, enemy.Height / 2f - 8);

		// Health potion drop
		if(_random.NextDouble() < enemy.HealthDropChance) {
			//var potion = new Pickup(PickupType.HealthPotion, dropPos, _healthPotionTexture);
			//_pickups.Add(potion);
		}

		// Coin drop
		if(_random.NextDouble() < enemy.CoinDropChance) {
			PickupType coinType = _random.NextDouble() < 0.2 ? PickupType.BigCoin : PickupType.Coin;
			Vector2 coinPos = dropPos + new Vector2(_random.Next(-10, 10), _random.Next(-10, 10));
			//_pickups.Add(new Pickup(coinType, coinPos, _coinTexture));
		}
	}

	public List<Pickup> CollectPickups(Player player) {
		var collected = new List<Pickup>();

		foreach(var pickup in _pickups) {
			//pickup.Update(gameTime);
			if(pickup.CheckCollision(player)) {
				player.CollectPickup(pickup);
				collected.Add(pickup);
			}
		}

		_pickups.RemoveAll(p => p.IsCollected);
		return collected;
	}
}