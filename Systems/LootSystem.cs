using EldmeresTale.Core;
using EldmeresTale.ECS.Factories;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class LootSystem : GameSystem {
	private readonly Player _player;
	private readonly AssetManager _assetManager;
	private readonly GraphicsDevice _graphicsDevice;
	private readonly GameEventBus _eventBus;
	private readonly List<Pickup> _pickups;
	private readonly Random _random;
	private readonly PickupFactory _pickupFactory;

	// Texture cache (loaded on-demand)
	private readonly Dictionary<string, Texture2D> _pickupTextures;

	public IReadOnlyList<Pickup> Pickups => _pickups;
	public int PickupCount => _pickups.Count;

	public LootSystem(Player player, AssetManager assetManager, GraphicsDevice graphicsDevice, GameEventBus eventBus, PickupFactory pickupFactory) {
		_player = player;
		_assetManager = assetManager;
		_graphicsDevice = graphicsDevice;
		_eventBus = eventBus;
		_pickups = [];
		_random = new Random();
		_pickupTextures = [];

		Enabled = true;
		Visible = false;
		_pickupFactory = pickupFactory;
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

	public override void Update(GameTime time) {
		if (!Enabled) {
			return;
		}

		// Update all pickups
		foreach (Pickup pickup in _pickups) {
			pickup.Update(time);

			// Check if player collects it
			if (pickup.CheckCollision(_player)) {
				_eventBus.Publish(new PickupCollectedEvent {
					Collector = _player,
					Pickup = pickup,
					Position = pickup.Position
				});
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
	public override void OnRoomChanged(Room newRoom) {
		// Clear all pickups from previous room
		Clear();

		System.Diagnostics.Debug.WriteLine($"[LOOT SYSTEM] Room changed - cleared {_pickups.Count} pickups");
	}

	public override void Dispose() {
		_pickups.Clear();
		System.Diagnostics.Debug.WriteLine("[LOOT SYSTEM] Disposed");
	}
}