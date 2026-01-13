using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace EldmeresTale.Worlds;

public class TileDefinition {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("drawOrder")]
	public int DrawOrder { get; set; }

	[JsonPropertyName("isWalkable")]
	public bool IsWalkable { get; set; }

	[JsonPropertyName("textureName")]
	public string TextureName { get; set; }

	[JsonPropertyName("color")]
	public string ColorHex { get; set; }

	[JsonIgnore]
	public Color MainColor { get; set; }

	public void ParseColors() {
		MainColor = ParseHexColor(ColorHex);
	}

	private static Color ParseHexColor(string hex) {
		if (string.IsNullOrEmpty(hex)) {
			return Color.White;
		}

		hex = hex.TrimStart('#');
		if (hex.Length == 6) {
			// RGB
			int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
			int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
			return new Color(r, g, b);
		} else if (hex.Length == 8) {
			// RGBA
			int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
			int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
			int a = int.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
			return new Color(r, g, b, a);
		}
		return Color.White;
	}
}
