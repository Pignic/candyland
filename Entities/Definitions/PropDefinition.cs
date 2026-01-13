using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EldmeresTale.Entities.Definitions;

public class PropDefinition {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("displayName")]
	public string DisplayName { get; set; }

	[JsonPropertyName("type")]
	public string TypeString { get; set; }

	[JsonPropertyName("width")]
	public int Width { get; set; } = 16;

	[JsonPropertyName("height")]
	public int Height { get; set; } = 16;

	[JsonPropertyName("health")]
	public int Health { get; set; } = 1;

	[JsonPropertyName("maxHealth")]
	public int MaxHealth { get; set; } = 1;

	[JsonPropertyName("pushSpeed")]
	public float PushSpeed { get; set; } = 80f;

	[JsonPropertyName("defaultColor")]
	public string DefaultColorHex { get; set; } = "#FFFFFF";

	[JsonIgnore]
	public Color DefaultColor {
		get => ParseColor(DefaultColorHex);
		set => DefaultColorHex = ColorToHex(value);
	}

	[JsonPropertyName("isCollidable")]
	public bool IsCollidable { get; set; } = true;

	[JsonPropertyName("lootTable")]
	public JsonElement[][] LootTableRaw { get; set; }

	[JsonIgnore]
	private Dictionary<string, float> LootTable;

	public Dictionary<string, float> GetLootTable(bool reload = false) {
		if (LootTable == null || reload) {
			LootTable = [];
			foreach (JsonElement[] value in LootTableRaw) {
				LootTable.TryAdd(value[0].GetString(), value[1].GetSingle());
			}
		}
		return LootTable;
	}

	public bool HasLootTable() {
		return LootTableRaw?.Length > 0;
	}

	[JsonPropertyName("category")]
	public string Category { get; set; } = "General";

	[JsonPropertyName("interactionText")]
	public string InteractionText { get; set; }

	// Color parsing helpers
	private static Color ParseColor(string hex) {
		if (string.IsNullOrEmpty(hex)) {
			return Color.White;
		}

		hex = hex.TrimStart('#');
		if (hex.Length == 6) {
			int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
			int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
			return new Color(r, g, b);
		}
		return Color.White;
	}

	private static string ColorToHex(Color color) {
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}