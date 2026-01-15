using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
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
	private static bool _initialized = false;
	public static Dictionary<string, PropDefinition> Catalog {
		get {
			if (!_initialized) {
				Initialize();
			}
			return _catalog;
		}
	}

	private const int _defaultPropWidth = 16;
	private const int _defaultPropHeight = 16;

	private readonly DefaultEcs.World _world;
	private readonly AssetManager _assetManager;



	public PropFactory(DefaultEcs.World world, AssetManager assetManager) {
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

	public Entity CreateStaticProp(string textureKey, Vector2 position, bool collidable = false, int width = _defaultPropWidth, int height = _defaultPropHeight) {
		Entity entity = _world.CreateEntity();

		entity.Set(new Position(position));
		entity.Set(new Sprite(_assetManager.LoadTexture($"Assets/Sprites/Props/{textureKey}.png")));
		if (collidable) {
			entity.Set(new Collider(width, height));
		}

		return entity;
	}

	public Entity CreateAnimatedProp(string textureKey, Vector2 position, int frameCount, int frameWidth, int frameHeight, float frameTime, bool collidable = false, bool pingPong = false) {
		Entity entity = _world.CreateEntity();

		entity.Set(new Position(position));
		Texture2D texture = _assetManager.LoadTexture($"Assets/Sprites/Props/{textureKey}.png");
		Components.Animation animation = new Components.Animation(
			texture,
			frameCount,
			frameWidth,
			frameHeight,
			frameTime,
			loop: true,
			pingPong: pingPong
		);
		entity.Set(animation);

		// Sprite will be updated by AnimationSystem
		entity.Set(new Sprite(texture) {
			SourceRect = animation.GetSourceRect()
		});

		if (collidable) {
			entity.Set(new Collider(frameWidth, frameHeight));
		}

		return entity;
	}

	public Entity CreateInteractiveProp(string textureKey, Vector2 position, string interactionId, bool collidable = true, int width = _defaultPropWidth, int height = _defaultPropHeight, float interactionRange = 50f) {
		Entity entity = _world.CreateEntity();

		entity.Set(new Position(position));
		entity.Set(new Sprite(_assetManager.LoadTexture($"Assets/Sprites/Props/{textureKey}.png")));
		entity.Set(new InteractionZone(interactionId, interactionRange));

		if (collidable) {
			entity.Set(new Collider(width, height));
		}

		return entity;
	}

	public Entity Create(string propId, Vector2 position) {
		Entity e = _world.CreateEntity();
		if (!_initialized) {
			Initialize();
		}

		if (!_catalog.TryGetValue(propId, out PropDefinition def)) {
			System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] Prop '{propId}' not found!");
			return e;
		}

		Texture2D propTexture = _assetManager.LoadTexture($"Assets/Sprites/Props/{def.Id}.png");

		e.Set(new Faction(FactionName.Prop));
		e.Set(new Health(def.Health));
		e.Set(new Sprite(propTexture));
		e.Set(new Position(position));
		e.Set(new Collider(def.Width, def.Height));
		if (def.HasLootTable()) {
			e.Set(new Lootable(def.GetLootTable()));
		}
		// TODO use interaction key
		e.Set(new InteractionZone(def.Id));

		return e;
	}

	// Convenience methods for common props

	public Entity CreateTree(Vector2 position) {
		return CreateStaticProp("tree", position, collidable: true, width: 32, height: 48);
	}

	public Entity CreateRock(Vector2 position) {
		return CreateStaticProp("rock", position, collidable: true, width: 24, height: 24);
	}

	public Entity CreateTorch(Vector2 position) {
		return CreateAnimatedProp("torch", position, frameCount: 4, frameWidth: 16, frameHeight: 32, frameTime: 0.15f);
	}

	public Entity CreateChest(Vector2 position, string lootId) {
		return CreateInteractiveProp("chest", position, $"chest_{lootId}", collidable: true);
	}

	public Entity CreateDoor(Vector2 position, string doorId) {
		return CreateInteractiveProp("door", position, $"door_{doorId}", collidable: true);
	}
}