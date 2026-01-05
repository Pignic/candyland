using EldmeresTale.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.Entities;

public static class PropFactory {
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

	public static void Initialize(string path = "Assets/Data/props.json") {
		_catalog = new Dictionary<string, PropDefinition>();

		try {
			if (!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] File not found: {path}");
				return;
			}

			string json = File.ReadAllText(path);
			PropCatalogData data = JsonSerializer.Deserialize<PropCatalogData>(json);

			if (data?.props == null) {
				System.Diagnostics.Debug.WriteLine("[PROP FACTORY] Invalid JSON format");
				return;
			}

			foreach (PropDefinition prop in data.props) {
				_catalog[prop.Id] = prop;
			}

			System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] Loaded {_catalog.Count} props from {path}");

		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] Error loading props: {ex.Message}");
		}

		_initialized = true;
	}

	public static Prop Create(string propId, Texture2D texture, Vector2 position, GraphicsDevice graphicsDevice = null) {
		if (!_initialized) {
			Initialize();
		}

		if (!_catalog.ContainsKey(propId)) {
			System.Diagnostics.Debug.WriteLine($"[PROP FACTORY] Prop '{propId}' not found!");
			return null;
		}

		PropDefinition def = _catalog[propId];

		Texture2D propTexture = texture ?? CreateFallbackTexture(graphicsDevice, def);

		Prop prop = new Prop(propTexture, position, def.Type, def.Width, def.Height) {
			health = def.Health,
			MaxHealth = def.MaxHealth,
			pushSpeed = def.PushSpeed,
			tint = def.DefaultColor,
			isCollidable = def.IsCollidable,
			lootTable = def.LootTable,
			lootChance = def.LootChance,
			interactionText = def.InteractionText
		};

		// Setup interactions from JSON
		SetupInteractions(prop, def);

		return prop;
	}

	private static void SetupInteractions(Prop prop, PropDefinition def) {
		switch (def.Id) {
			case "chest":
				prop.onInteract = (p) => {
					p.interactionText = "Empty";
					System.Diagnostics.Debug.WriteLine("Chest opened!");
				};
				break;
			case "lever":
				bool leverActive = false;
				prop.onInteract = (p) => {
					leverActive = !leverActive;
					p.tint = leverActive ? Color.Yellow : Color.Silver;
					System.Diagnostics.Debug.WriteLine($"Lever: {leverActive}");
				};
				break;
			case "sign":
				prop.onInteract = (p) => {
					System.Diagnostics.Debug.WriteLine("Sign: Hello adventurer!");
				};
				break;
			case "door_locked":
				prop.onInteract = (p) => {
					System.Diagnostics.Debug.WriteLine("Door is locked!");
				};
				break;
		}
	}

	public static List<string> GetPropsByCategory(string category) {
		if (!_initialized) {
			Initialize();
		}

		List<string> props = new List<string>();
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

		HashSet<string> categories = new HashSet<string>();
		foreach (PropDefinition def in _catalog.Values) {
			categories.Add(def.Category);
		}

		return new List<string>(categories);
	}

	private static Texture2D CreateFallbackTexture(GraphicsDevice graphicsDevice, PropDefinition def) {
		if (graphicsDevice == null) {
			return null;
		}

		return Graphics.CreateColoredTexture(graphicsDevice, def.Width, def.Height, def.DefaultColor);
	}

	private class PropCatalogData {
		public List<PropDefinition> props { get; set; }
	}
}