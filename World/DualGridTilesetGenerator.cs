using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Candyland.World {
	/// <summary>
	/// Generates 16-tile dual-grid tilesets procedurally
	/// Creates a 64x64 texture with all 16 transition tiles
	/// </summary>
	public static class DualGridTilesetGenerator {
		/// <summary>
		/// Generate a complete 16-tile tileset for a terrain type
		/// Returns a 64x64 texture (4x4 grid of 16x16 tiles)
		/// </summary>
		public static Texture2D GenerateTileset(GraphicsDevice graphicsDevice, TileType terrainType, int tileSize = 16) {
			int textureSize = tileSize * 4; // 4x4 grid = 64x64 total
			Texture2D tileset = new Texture2D(graphicsDevice, textureSize, textureSize);
			Color[] pixels = new Color[textureSize * textureSize];

			// Get colors for this terrain
			Color mainColor = GetMainColor(terrainType);
			Color darkColor = GetDarkColor(terrainType);
			Color lightColor = GetLightColor(terrainType);
			Color borderColor = GetBorderColor(terrainType);

			// Generate all 16 tiles
			for(int mask = 0; mask < 16; mask++) {
				int tileColumn = mask % 4;
				int tileRow = mask / 4;

				GenerateTile(
					pixels,
					textureSize,
					tileColumn * tileSize,
					tileRow * tileSize,
					tileSize,
					mask,
					mainColor,
					darkColor,
					lightColor,
					borderColor
				);
			}

			tileset.SetData(pixels);
			return tileset;
		}

		/// <summary>
		/// Generate a single tile based on bitmask
		/// </summary>
		private static void GenerateTile(
			Color[] pixels,
			int textureWidth,
			int startX,
			int startY,
			int tileSize,
			int mask,
			Color mainColor,
			Color darkColor,
			Color lightColor,
			Color borderColor) {
			// Bitmask: [TL TR BL BR]
			// Bit 3 (8): Top-Left
			// Bit 2 (4): Top-Right
			// Bit 1 (2): Bottom-Left
			// Bit 0 (1): Bottom-Right

			bool topLeft = (mask & 8) != 0;
			bool topRight = (mask & 4) != 0;
			bool bottomLeft = (mask & 2) != 0;
			bool bottomRight = (mask & 1) != 0;

			int halfSize = tileSize / 2;

			// Draw tile pixel by pixel
			for(int y = 0; y < tileSize; y++) {
				for(int x = 0; x < tileSize; x++) {
					int pixelX = startX + x;
					int pixelY = startY + y;
					int index = pixelY * textureWidth + pixelX;

					// Determine which quadrant this pixel is in
					bool inTopHalf = y < halfSize;
					bool inLeftHalf = x < halfSize;

					bool shouldFill = false;

					if(inTopHalf && inLeftHalf)
						shouldFill = topLeft;
					else if(inTopHalf && !inLeftHalf)
						shouldFill = topRight;
					else if(!inTopHalf && inLeftHalf)
						shouldFill = bottomLeft;
					else
						shouldFill = bottomRight;

					if(shouldFill) {
						// Add some texture/pattern
						Color pixelColor = GetTexturedPixel(x, y, tileSize, mainColor, darkColor, lightColor);
						pixels[index] = pixelColor;
					} else {
						// Transparent or background
						pixels[index] = Color.Transparent;
					}

					// Add border on edges between filled and empty
					if(ShouldDrawBorder(x, y, halfSize, topLeft, topRight, bottomLeft, bottomRight)) {
						pixels[index] = borderColor;
					}
				}
			}
		}

		/// <summary>
		/// Check if we should draw a border pixel
		/// </summary>
		private static bool ShouldDrawBorder(int x, int y, int halfSize, bool tl, bool tr, bool bl, bool br) {
			bool inTopHalf = y < halfSize;
			bool inLeftHalf = x < halfSize;
			bool onHorizontalEdge = y == halfSize - 1 || y == halfSize;
			bool onVerticalEdge = x == halfSize - 1 || x == halfSize;

			// Draw border where filled meets empty
			if(onHorizontalEdge && inLeftHalf && tl != bl) return true;
			if(onHorizontalEdge && !inLeftHalf && tr != br) return true;
			if(onVerticalEdge && inTopHalf && tl != tr) return true;
			if(onVerticalEdge && !inTopHalf && bl != br) return true;

			return false;
		}

		/// <summary>
		/// Add texture/pattern to pixels for more interesting look
		/// </summary>
		private static Color GetTexturedPixel(int x, int y, int tileSize, Color main, Color dark, Color light) {
			// Simple noise-like pattern
			int hash = (x * 374761393 + y * 668265263) % 100;

			if(hash < 10)
				return dark;
			else if(hash < 20)
				return light;
			else
				return main;
		}

		/// <summary>
		/// Get main color for terrain type
		/// </summary>
		private static Color GetMainColor(TileType terrainType) {
			return terrainType switch {
				TileType.Grass => new Color(50, 180, 50),
				TileType.Water => new Color(40, 150, 255),
				TileType.Stone => new Color(140, 140, 140),
				TileType.Tree => new Color(20, 120, 20),
				_ => Color.White
			};
		}

		/// <summary>
		/// Get dark shade for terrain type
		/// </summary>
		private static Color GetDarkColor(TileType terrainType) {
			return terrainType switch {
				TileType.Grass => new Color(30, 140, 30),
				TileType.Water => new Color(20, 100, 200),
				TileType.Stone => new Color(100, 100, 100),
				TileType.Tree => new Color(10, 80, 10),
				_ => Color.Gray
			};
		}

		/// <summary>
		/// Get light shade for terrain type
		/// </summary>
		private static Color GetLightColor(TileType terrainType) {
			return terrainType switch {
				TileType.Grass => new Color(70, 220, 70),
				TileType.Water => new Color(60, 180, 255),
				TileType.Stone => new Color(180, 180, 180),
				TileType.Tree => new Color(30, 160, 30),
				_ => Color.LightGray
			};
		}

		/// <summary>
		/// Get border color for terrain type
		/// </summary>
		private static Color GetBorderColor(TileType terrainType) {
			return terrainType switch {
				TileType.Grass => new Color(20, 100, 20),
				TileType.Water => new Color(10, 80, 180),
				TileType.Stone => new Color(80, 80, 80),
				TileType.Tree => new Color(5, 60, 5),
				_ => Color.Black
			};
		}
	}
}