using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.World {

	public static class DualGridTilesetGenerator {

		public static Texture2D GenerateTileset(GraphicsDevice graphicsDevice, TileType terrainType, int tileSize = 16) {
			int textureSize = tileSize * 4; // 4x4 grid = 64x64 total
			int textureWidth = textureSize;
			int textureHeight = textureSize + tileSize; // 64x80 total
			Texture2D tileset = new Texture2D(graphicsDevice, textureWidth, textureHeight);
			Color[] pixels = new Color[textureWidth * textureHeight];

			// Get colors for this terrain
			Color mainColor = GetMainColor(terrainType);
			Color darkColor = GetDarkColor(terrainType);
			Color lightColor = GetLightColor(terrainType);
			Color borderColor = GetBorderColor(terrainType);
			Color variationColor = GetVariationColor(terrainType);

			int[,] layout = {
				{ 13, 10,  4, 12 },  // Row 0
				{  6,  8,  0,  1 },  // Row 1
				{ 11,  3,  2,  5 },  // Row 2
				{ 15, 14,  9,  7 }   // Row 3
			};

			// Generate all 16 tiles
			for(int row = 0; row < 4; row++) {
				for(int col = 0; col < 4; col++) {
					int mask = layout[row, col];

					GenerateTile(
						pixels,
						textureWidth,
						col * tileSize,
						row * tileSize,
						tileSize,
						mask,
						mainColor,
						darkColor,
						lightColor,
						borderColor
					);
				}
			}

			for(int i = 0; i < 4; i++) {
				GenerateVariationTile(
					pixels,
					textureWidth,
					i * tileSize,        // X position
					textureSize,         // Y position (64 pixels down)
					tileSize,
					i,                   // Variation index
					variationColor,
					darkColor
				);
			}

			tileset.SetData(pixels);
			return tileset;
		}

		private static void GenerateVariationTile(
	Color[] pixels,
	int textureWidth,
	int startX,
	int startY,
	int tileSize,
	int variationIndex,
	Color detailColor,
	Color darkColor) {
			// Create sparse random details (mostly transparent)
			for(int y = 0; y < tileSize; y++) {
				for(int x = 0; x < tileSize; x++) {
					int pixelX = startX + x;
					int pixelY = startY + y;
					int index = pixelY * textureWidth + pixelX;

					// Use hash for pseudo-random but consistent pattern
					int hash = (x * 374761393 + y * 668265263 + variationIndex * 1000) % 100;

					if(hash < 8)  // 8% chance for detail pixel
					{
						pixels[index] = detailColor;
					} else if(hash < 15)  // Additional 7% for darker details
					  {
						pixels[index] = darkColor;
					} else {
						pixels[index] = Color.Transparent;
					}
				}
			}
		}

		private static void GenerateVariationTile_Detailed(
				Color[] pixels,
				int textureWidth,
				int startX,
				int startY,
				int tileSize,
				int variationIndex,
				Color detailColor) {
			// Variation 0: Top-left cluster
			if(variationIndex == 0) {
				for(int y = 2; y < 5; y++)
					for(int x = 2; x < 5; x++)
						SetPixel(pixels, textureWidth, startX + x, startY + y, detailColor);
			}
			// Variation 1: Bottom-right cluster
			else if(variationIndex == 1) {
				for(int y = 11; y < 14; y++)
					for(int x = 11; x < 14; x++)
						SetPixel(pixels, textureWidth, startX + x, startY + y, detailColor);
			}
			// Variation 2: Diagonal line
			else if(variationIndex == 2) {
				for(int i = 0; i < tileSize; i += 4)
					SetPixel(pixels, textureWidth, startX + i, startY + i, detailColor);
			}
			// Variation 3: Center dot
			else if(variationIndex == 3) {
				for(int y = 7; y < 9; y++)
					for(int x = 7; x < 9; x++)
						SetPixel(pixels, textureWidth, startX + x, startY + y, detailColor);
			}
		}

		private static void SetPixel(Color[] pixels, int textureWidth, int x, int y, Color color) {
			int index = y * textureWidth + x;
			if(index >= 0 && index < pixels.Length)
				pixels[index] = color;
		}

		private static Color GetVariationColor(TileType terrainType) {
			return terrainType switch {
				TileType.Grass => new Color(40, 160, 40),   // Darker grass specs
				TileType.Water => new Color(30, 130, 220),  // Water sparkles
				TileType.Stone => new Color(120, 120, 120), // Stone specs
				TileType.Tree => new Color(15, 100, 15),    // Dark tree details
				_ => Color.Gray
			};
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