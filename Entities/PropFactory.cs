using Candyland.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Candyland.Entities {
	/// <summary>
	/// Defines a prop template for the catalog
	/// </summary>
	public class PropDefinition {
		public string Id { get; set; }
		public string DisplayName { get; set; }
		public PropType Type { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int Health { get; set; } = 1;
		public int MaxHealth { get; set; } = 1;
		public float PushSpeed { get; set; } = 80f;
		public string SpriteKey { get; set; }
		public Color DefaultColor { get; set; } = Color.White;
		public bool IsCollidable { get; set; } = true;
		public string[] LootTable { get; set; }
		public float LootChance { get; set; } = 0.5f;
		public string Category { get; set; } = "General";
	}

	/// <summary>
	/// Factory for creating props with string identifiers
	/// </summary>
	public static class PropFactory {
		private static Dictionary<string, PropDefinition> _catalog;
		private static bool _initialized = false;

		/// <summary>
		/// Get the complete prop catalog
		/// </summary>
		public static Dictionary<string, PropDefinition> Catalog {
			get {
				if(!_initialized)
					InitializeCatalog();
				return _catalog;
			}
		}

		/// <summary>
		/// Create a prop by string identifier
		/// </summary>
		public static Prop Create(string propId, Texture2D texture, Vector2 position, GraphicsDevice graphicsDevice = null) {
			if(!_initialized)
				InitializeCatalog();

			if(!_catalog.ContainsKey(propId)) {
				System.Diagnostics.Debug.WriteLine($"Warning: Prop '{propId}' not found in catalog!");
				return null;
			}

			var definition = _catalog[propId];

			// Use provided texture or create fallback
			Texture2D propTexture = texture ?? CreateFallbackTexture(graphicsDevice, definition);

			var prop = new Prop(propTexture, position, definition.Type, definition.Width, definition.Height) {
				health = definition.Health,
				MaxHealth = definition.MaxHealth,
				pushSpeed = definition.PushSpeed,
				tint = definition.DefaultColor,
				isCollidable = definition.IsCollidable,
				lootTable = definition.LootTable,
				lootChance = definition.LootChance
			};

			// Apply type-specific setup
			SetupPropByType(prop, propId);

			return prop;
		}

		/// <summary>
		/// Get all prop IDs in a category
		/// </summary>
		public static List<string> GetPropsByCategory(string category) {
			if(!_initialized)
				InitializeCatalog();

			var props = new List<string>();
			foreach(var kvp in _catalog) {
				if(kvp.Value.Category == category)
					props.Add(kvp.Key);
			}
			return props;
		}

		/// <summary>
		/// Get all categories
		/// </summary>
		public static List<string> GetCategories() {
			if(!_initialized)
				InitializeCatalog();

			var categories = new HashSet<string>();
			foreach(var def in _catalog.Values)
				categories.Add(def.Category);

			return new List<string>(categories);
		}

		// ================================================================
		// CATALOG INITIALIZATION
		// ================================================================

		private static void InitializeCatalog() {
			_catalog = new Dictionary<string, PropDefinition>();

			// === BREAKABLES ===
			_catalog["crate"] = new PropDefinition {
				Id = "crate",
				DisplayName = "Crate",
				Type = PropType.Breakable,
				Width = 16,
				Height = 16,
				Health = 3,
				MaxHealth = 3,
				DefaultColor = new Color(139, 69, 19),
				LootTable = new[] { "coin", "health_potion" },
				LootChance = 0.6f,
				Category = "Breakable"
			};

			_catalog["barrel"] = new PropDefinition {
				Id = "barrel",
				DisplayName = "Barrel",
				Type = PropType.Breakable,
				Width = 16,
				Height = 20,
				Health = 5,
				MaxHealth = 5,
				DefaultColor = new Color(101, 67, 33),
				LootTable = new[] { "coin", "coin", "coin" },
				LootChance = 0.8f,
				Category = "Breakable"
			};

			_catalog["pot"] = new PropDefinition {
				Id = "pot",
				DisplayName = "Pot",
				Type = PropType.Breakable,
				Width = 12,
				Height = 14,
				Health = 1,
				MaxHealth = 1,
				DefaultColor = new Color(178, 102, 51),
				LootTable = new[] { "heart" },
				LootChance = 0.3f,
				Category = "Breakable"
			};

			_catalog["crystal"] = new PropDefinition {
				Id = "crystal",
				DisplayName = "Crystal",
				Type = PropType.Breakable,
				Width = 14,
				Height = 18,
				Health = 2,
				MaxHealth = 2,
				DefaultColor = Color.Cyan,
				LootTable = new[] { "gem" },
				LootChance = 1.0f,
				Category = "Breakable"
			};

			// === PUSHABLES ===
			_catalog["box"] = new PropDefinition {
				Id = "box",
				DisplayName = "Box",
				Type = PropType.Pushable,
				Width = 16,
				Height = 16,
				PushSpeed = 80f,
				DefaultColor = new Color(210, 180, 140),
				Category = "Pushable"
			};

			_catalog["boulder"] = new PropDefinition {
				Id = "boulder",
				DisplayName = "Boulder",
				Type = PropType.Pushable,
				Width = 20,
				Height = 20,
				PushSpeed = 40f,
				DefaultColor = Color.Gray,
				Category = "Pushable"
			};

			_catalog["ice_block"] = new PropDefinition {
				Id = "ice_block",
				DisplayName = "Ice Block",
				Type = PropType.Pushable,
				Width = 16,
				Height = 16,
				PushSpeed = 120f,  // Slides fast!
				DefaultColor = Color.LightBlue,
				Category = "Pushable"
			};

			// === INTERACTIVE ===
			_catalog["chest"] = new PropDefinition {
				Id = "chest",
				DisplayName = "Chest",
				Type = PropType.Interactive,
				Width = 16,
				Height = 16,
				DefaultColor = Color.Gold,
				Category = "Interactive"
			};

			_catalog["lever"] = new PropDefinition {
				Id = "lever",
				DisplayName = "Lever",
				Type = PropType.Interactive,
				Width = 8,
				Height = 16,
				DefaultColor = Color.Silver,
				Category = "Interactive"
			};

			_catalog["sign"] = new PropDefinition {
				Id = "sign",
				DisplayName = "Sign",
				Type = PropType.Interactive,
				Width = 16,
				Height = 24,
				DefaultColor = new Color(139, 69, 19),
				Category = "Interactive"
			};

			_catalog["door_locked"] = new PropDefinition {
				Id = "door_locked",
				DisplayName = "Locked Door",
				Type = PropType.Interactive,
				Width = 16,
				Height = 32,
				DefaultColor = new Color(101, 67, 33),
				Category = "Interactive"
			};

			_catalog["button"] = new PropDefinition {
				Id = "button",
				DisplayName = "Floor Button",
				Type = PropType.Interactive,
				Width = 16,
				Height = 8,
				DefaultColor = Color.Red,
				IsCollidable = false,
				Category = "Interactive"
			};

			// === STATIC ===
			_catalog["rock"] = new PropDefinition {
				Id = "rock",
				DisplayName = "Rock",
				Type = PropType.Static,
				Width = 16,
				Height = 16,
				DefaultColor = Color.Gray,
				Category = "Static"
			};

			_catalog["statue"] = new PropDefinition {
				Id = "statue",
				DisplayName = "Statue",
				Type = PropType.Static,
				Width = 24,
				Height = 32,
				DefaultColor = Color.LightGray,
				Category = "Static"
			};

			_catalog["plant"] = new PropDefinition {
				Id = "plant",
				DisplayName = "Plant",
				Type = PropType.Static,
				Width = 12,
				Height = 16,
				DefaultColor = Color.Green,
				IsCollidable = false,
				Category = "Static"
			};

			_catalog["tree_stump"] = new PropDefinition {
				Id = "tree_stump",
				DisplayName = "Tree Stump",
				Type = PropType.Static,
				Width = 20,
				Height = 16,
				DefaultColor = new Color(101, 67, 33),
				Category = "Static"
			};

			_catalog["pillar"] = new PropDefinition {
				Id = "pillar",
				DisplayName = "Pillar",
				Type = PropType.Static,
				Width = 16,
				Height = 32,
				DefaultColor = Color.LightGray,
				Category = "Static"
			};

			// === COLLECTIBLES ===
			_catalog["coin"] = new PropDefinition {
				Id = "coin",
				DisplayName = "Coin",
				Type = PropType.Collectible,
				Width = 8,
				Height = 8,
				DefaultColor = Color.Gold,
				IsCollidable = false,
				Category = "Collectible"
			};

			_catalog["heart"] = new PropDefinition {
				Id = "heart",
				DisplayName = "Heart",
				Type = PropType.Collectible,
				Width = 12,
				Height = 12,
				DefaultColor = Color.Red,
				IsCollidable = false,
				Category = "Collectible"
			};

			_catalog["gem"] = new PropDefinition {
				Id = "gem",
				DisplayName = "Gem",
				Type = PropType.Collectible,
				Width = 10,
				Height = 10,
				DefaultColor = Color.Cyan,
				IsCollidable = false,
				Category = "Collectible"
			};

			_initialized = true;
		}

		// ================================================================
		// HELPER METHODS
		// ================================================================

		private static void SetupPropByType(Prop prop, string propId) {
			switch(propId) {
				case "chest":
					prop.interactionText = "Press E to open";
					prop.onInteract = (p) => {
						p.interactionText = "Empty";
						System.Diagnostics.Debug.WriteLine("Chest opened!");
					};
					break;

				case "lever":
					bool leverActive = false;
					prop.interactionText = "Press E to pull";
					prop.onInteract = (p) => {
						leverActive = !leverActive;
						p.tint = leverActive ? Color.Yellow : Color.Silver;
						System.Diagnostics.Debug.WriteLine($"Lever: {leverActive}");
					};
					break;

				case "sign":
					prop.interactionText = "Press E to read";
					prop.onInteract = (p) => {
						System.Diagnostics.Debug.WriteLine("Sign: Hello adventurer!");
					};
					break;

				case "door_locked":
					prop.interactionText = "Locked";
					prop.onInteract = (p) => {
						System.Diagnostics.Debug.WriteLine("Door is locked!");
					};
					break;

				case "button":
					prop.interactionText = "Step on to activate";
					break;
			}
		}

		private static Texture2D CreateFallbackTexture(GraphicsDevice graphicsDevice, PropDefinition definition) {
			if(graphicsDevice == null)
				return null;

			return Graphics.CreateColoredTexture(
				graphicsDevice,
				definition.Width,
				definition.Height,
				definition.DefaultColor
			);
		}
	}
}