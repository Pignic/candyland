using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.Entities.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.ECS.Factories;

public class PropFactory {

	private static Dictionary<string, PropDefinition> _catalog;
	private static bool _initialized;

	public static Dictionary<string, PropDefinition> Catalog {
		get {
			if (!_initialized) {
				Initialize();
			}
			return _catalog;
		}
	}

	private readonly World _world;
	private readonly AssetManager _assetManager;

	public PropFactory(World world, AssetManager assetManager) {
		_world = world;
		_assetManager = assetManager;
	}

	public static void Initialize(string path = "Assets/Data/props.json") {
		_catalog = [];

		try {
			if (!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] File not found: {path}");
				return;
			}

			string json = File.ReadAllText(path);
			PropCatalogData data = JsonSerializer.Deserialize<PropCatalogData>(json);

			if (data?.Props == null) {
				System.Diagnostics.Debug.WriteLine("[PROP FACTORY] Invalid JSON format");
				return;
			}

			foreach (PropDefinition prop in data.Props) {
				_catalog[prop.Id] = prop;
			}

			System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] Loaded {_catalog.Count} props from {path}");

		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] Error loading props: {ex.Message}");
		}

		_initialized = true;
	}

	private class PropCatalogData {
		public List<PropDefinition> Props { get; set; }
	}


	public static List<string> GetPropsByCategory(string category) {
		if (!_initialized) {
			Initialize();
		}

		List<string> props = [];
		foreach (KeyValuePair<string, PropDefinition> kvp in _catalog) {
			if (kvp.Value.Category == category) {
				props.Add(kvp.Key);
			}
		}
		return props;
	}

	public static List<string> GetCategories() {
		if (!_initialized) {
			Initialize();
		}

		HashSet<string> categories = [];
		foreach (PropDefinition def in _catalog.Values) {
			categories.Add(def.Category);
		}

		return [.. categories];
	}

	public Entity Create(string roomId, string propId, Vector2 position) {
		Entity e = _world.CreateEntity();
		if (!_initialized) {
			Initialize();
		}

		if (!_catalog.TryGetValue(propId, out PropDefinition def)) {
			System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] Prop '{propId}' not found!");
			return e;
		}

		e.Set(new RoomId(roomId));

		Texture2D propTexture = _assetManager.LoadTexture($"Assets/Sprites/Props/{def.Id}.png");
		e.Set(new Faction(FactionName.Prop));
		e.Set(new DefinitionId(propId));
		e.Set(new Health(def.Health));
		e.Set(new Sprite(propTexture));
		e.Set(new Position(position));
		if (def.IsCollidable) {
			e.Set(new Collider(def.Width, def.Height));
		}
		if (def.HasLootTable()) {
			e.Set(new Lootable(def.GetLootTable(), def.XpValue, def.CoinValue));
		}
		if (def.InteractionKey != null) {
			e.Set(new InteractionZone(def.Id));
		}

		return e;
	}
}