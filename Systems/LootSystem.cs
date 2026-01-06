using EldmeresTale.Core;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class LootSystem : GameSystem {
	private readonly Player _player;
	private readonly AssetManager _assetManager;
	private readonly GraphicsDevice _graphicsDevice;
	private readonly List<Pickup> _pickups;
	private readonly Random _random;

	// Texture cache (loaded on-demand)
	private readonly Dictionary<string, Texture2D> _pickupTextures;

	// Events
	public event Action<Pickup> OnPickupSpawned;
	public event Action<Pickup> OnPickupCollected;

	public IReadOnlyList<Pickup> Pickups => _pickups;
	public int PickupCount => _pickups.Count;

	public LootSystem(Player player, AssetManager assetManager, GraphicsDevice graphicsDevice) {
		_player = player;
		_assetManager = assetManager;
		_graphicsDevice = graphicsDevice;
		_pickups = new List<Pickup>();
		_random = new Random();
		_pickupTextures = new Dictionary<string, Texture2D>();

		Enabled = true;
		Visible = false;
	}

	public override void Initialize() {
		// Load default textures
		LoadDefaultTextures();
		System.Diagnostics.Debug.WriteLine("[LOOT SYSTEM] Initialized");
	}

	// TODO: use asset manager for that
	private void LoadDefaultTextures() {
		// Health potion
		_pickupTextures["health_potion"] = LoadOrCreateTexture(
			"Assets/Items/health_potion.png",
			16, 16,
			Color.LimeGreen
		);

		// Coin (small)
		_pickupTextures["coin"] = LoadOrCreateTexture(
			"Assets/Items/coin.png",
			6, 6,
			Color.Gold
		);

		// Big coin
		_pickupTextures["big_coin"] = LoadOrCreateTexture(
			"Assets/Items/big_coin.png",
			8, 8,
			Color.Gold
		);
	}

	private Texture2D LoadOrCreateTexture(string path, int width, int height, Color fallbackColor) {
		Texture2D texture = _assetManager.LoadTexture(path);
		if (texture != null) {
			return texture;
		}

		// Fallback: create colored texture
		System.Diagnostics.Debug.WriteLine($"[LOOT SYSTEM] Texture not found: {path}, using fallback");
		return Graphics.CreateColoredTexture(_graphicsDevice, width, height, fallbackColor);
	}

	public void SpawnLootFromEnemy(Enemy enemy) {
		Vector2 dropPos = enemy.Position + new Vector2((enemy.Width / 2f) - 8, (enemy.Height / 2f) - 8);
		if (_random.NextDouble() < enemy.CoinDropChance) {
			int coinAmount = enemy.RollCoinDrop(_random);
			while (coinAmount > 0) {
				// Add slight random offset so coins don't stack
				Vector2 coinPos = dropPos + new Vector2(
					_random.Next(-10, 10),
					_random.Next(-10, 10)
				);
				if (coinAmount > 5 && _random.NextDouble() > 0.2) {
					SpawnPickup(PickupType.BigCoin, coinPos);
					coinAmount -= 5;
				} else {
					SpawnPickup(PickupType.Coin, coinPos);
					coinAmount -= 1;
				}
			}
		}

		// Health potion
		if (_random.NextDouble() < enemy.HealthDropChance) {
			SpawnPickup(PickupType.HealthPotion, dropPos);
		}

		// Loot table (equipment, quest items)
		string itemId = enemy.RollLootDrop(_random);
		if (itemId != null) {
			// TODO drop some equipment/items
			//SpawnEquipment(itemId, dropPos);  // Create from EquipmentFactory!
		}
	}

	public void SpawnPickup(PickupType type, Vector2 position) {
		// Get texture for this pickup type
		string textureName = type switch {
			PickupType.HealthPotion => "health_potion",
			PickupType.Coin => "coin",
			PickupType.BigCoin => "big_coin",
			_ => "coin" // Default fallback
		};

		if (!_pickupTextures.ContainsKey(textureName)) {
			System.Diagnostics.Debug.WriteLine($"[LOOT SYSTEM] Warning: No texture for {textureName}");
			return;
		}

		Texture2D texture = _pickupTextures[textureName];
		Pickup pickup = new Pickup(type, position, texture);

		_pickups.Add(pickup);
		OnPickupSpawned?.Invoke(pickup);

		System.Diagnostics.Debug.WriteLine($"[LOOT SYSTEM] Spawned {type} at {position}");
	}

	public override void Update(GameTime time) {
		if (!Enabled) {
			return;
		}

		// Update all pickups
		foreach (Pickup pickup in _pickups) {
			pickup.Update(time);

			// Check if player collects it
			if (pickup.CheckCollision(_player)) {
				OnPickupCollected?.Invoke(pickup);
			}
		}

		// Remove collected pickups
		_pickups.RemoveAll(p => p.IsCollected);
	}

	public void Clear() {
		_pickups.Clear();
		System.Diagnostics.Debug.WriteLine("[LOOT SYSTEM] Cleared all pickups");
	}

	public override void Draw(SpriteBatch spriteBatch) {

	}

	public override void Dispose() {
		_pickups.Clear();
		OnPickupSpawned = null;
		OnPickupCollected = null;
		System.Diagnostics.Debug.WriteLine("[LOOT SYSTEM] Disposed");
	}
}