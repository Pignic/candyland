using Microsoft.Xna.Framework;

namespace Candyland.World;

public enum TileType {
	Grass,
	Water,
	Stone,
	Tree
}

public class Tile {
	public TileType Type { get; set; }
	public bool IsWalkable { get; set; }
	public Color Color { get; set; }

	public Tile(TileType type) {
		Type = type;

		// Set properties based on type
		switch(type) {
			case TileType.Grass:
				IsWalkable = true;
				Color = new Color(34, 139, 34); // Forest green
				break;
			case TileType.Water:
				IsWalkable = false;
				Color = new Color(30, 144, 255); // Dodger blue
				break;
			case TileType.Stone:
				IsWalkable = true;
				Color = new Color(128, 128, 128); // Gray
				break;
			case TileType.Tree:
				IsWalkable = false;
				Color = new Color(0, 100, 0); // Dark green
				break;
		}
	}
}