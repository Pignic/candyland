using EldmoresTale.World;
using Microsoft.Xna.Framework;

namespace EldmeresTale.World;

public enum TileType {
	Grass,
	Water,
	Stone,
	Tree
}

public class Tile {
	public TileType Type { get; set; }
	public bool IsWalkable { get; private set; }
	public Color Color { get; private set; }

	public Tile(TileType type) {
		Type = type;

		// Load properties from registry
		TileDefinition definition = TileRegistry.Instance.GetTile(type);
		if (definition != null) {
			IsWalkable = definition.IsWalkable;
			Color = definition.MainColor;
		} else {
			// Fallback
			IsWalkable = true;
			Color = Color.White;
		}
	}
}