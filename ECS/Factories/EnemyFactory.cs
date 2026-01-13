using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.Entities.Definitions;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.ECS.Factories;

public class EnemyFactory {
	private static readonly Dictionary<string, EnemyDefinition> _catalog = [];
	private static bool _initialized = false;
	public static Dictionary<string, EnemyDefinition> Catalog {
		get {
			if (!_initialized) {
				Initialize();
			}
			return _catalog;
		}
	}
	private readonly World _world;
	private readonly AssetManager _assetManager;

	public EnemyFactory(World world, AssetManager assetManager) {
		_world = world;
		_assetManager = assetManager;
	}

	public static void Initialize(string path = "Assets/Data/enemies.json") {
		_catalog.Clear();

		try {
			if (!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[ENEMY FACTORY] File not found: {path}");
				return;
			}

			string json = File.ReadAllText(path);
			EnemyCatalogData data = JsonSerializer.Deserialize<EnemyCatalogData>(json);

			if (data?.Enemies == null) {
				System.Diagnostics.Debug.WriteLine("[ENEMY FACTORY] Invalid JSON format");
				return;
			}

			// First pass: Load all base enemies
			foreach (EnemyDefinition enemy in data.Enemies) {
				if (string.IsNullOrEmpty(enemy.BaseId)) {
					_catalog[enemy.Id] = enemy;
				}
			}

			// Second pass: Load variants (inherit from base)
			foreach (EnemyDefinition enemy in data.Enemies) {
				if (!string.IsNullOrEmpty(enemy.BaseId)) {
					if (_catalog.TryGetValue(enemy.BaseId, out EnemyDefinition value)) {
						enemy.InheritFrom(value);
						_catalog[enemy.Id] = enemy;
					} else {
						System.Diagnostics.Debug.WriteLine($"[ENEMY FACTORY] Base enemy not found: {enemy.BaseId}");
					}
				}
			}

			System.Diagnostics.Debug.WriteLine($"[ENEMY FACTORY] Loaded {_catalog.Count} enemies from {path}");

		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[ENEMY FACTORY] Error: {ex.Message}");
		}

		_initialized = true;
	}

	public Entity Create(EnemySpawnData spawnData) {
		Entity e = _world.CreateEntity();
		if (!_initialized) {
			Initialize();
		}

		if (!_catalog.TryGetValue(spawnData.EnemyId, out EnemyDefinition def)) {
			System.Diagnostics.Debug.WriteLine($"[ENEMY FACTORY] Enemy '{spawnData.EnemyId}' not found!");
			return e;
		}

		Texture2D enemyTexture = _assetManager.LoadTexture($"Assets/Sprites/Actors/{def.Id}.png");

		e.Set(new Health(def.Health));
		e.Set(new Sprite(enemyTexture));
		e.Set(new Position(spawnData.X, spawnData.Y));
		e.Set(new Collider(def.Width, def.Height));
		e.Set(new Components.Animation(
			enemyTexture, def.FrameCount, def.FrameWidth, def.FrameHeight, def.FrameTime, true, false
		));
		if (def.HasLootTable()) {
			e.Set(new Lootable(def.GetLootTable()));
		}
		e.Set(new AIBehavior(def.Behavior, def.DetectionRange));

		return e;
	}

	public Entity Create(string enemyId, Vector2 position) {
		return Create(new EnemySpawnData() {
			EnemyId = enemyId,
			X = position.X,
			Y = position.Y
		});
	}

	private class EnemyCatalogData {
		public List<EnemyDefinition> Enemies { get; set; }
	}
}
