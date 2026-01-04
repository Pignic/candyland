using EldmoresTale.World;
using Microsoft.Xna.Framework;

namespace EldmeresTale.World;

public class Tile {
	public string TileId { get; set; }
	public bool IsWalkable { get; private set; }
	public Color Color { get; private set; }

	public Tile(string tileId) {
		TileId = tileId;

		// Load properties from registry
		TileDefinition definition = TileRegistry.Instance.GetTile(TileId);
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