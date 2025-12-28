using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.World.Tools;

public static class TilesetGenerator {

	public static Texture2D generateTileset(GraphicsDevice graphicsDevice, TileType terrainType, int tileSize = 16) {
		int textureSize = tileSize * 4; // 4x4 grid = 64x64 total
		int textureWidth = textureSize;
		int textureHeight = textureSize + tileSize; // 64x80 total
		Texture2D tileset = new Texture2D(graphicsDevice, textureWidth, textureHeight);
		Color[] pixels = new Color[textureWidth * textureHeight];

		// Get colors for this terrain
		Color mainColor = getMainColor(terrainType);
		Color darkColor = getDarkColor(terrainType);
		Color lightColor = getLightColor(terrainType);
		Color borderColor = getBorderColor(terrainType);
		Color variationColor = getVariationColor(terrainType);

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
				generateTile(pixels, textureWidth, col * tileSize, row * tileSize, tileSize, mask, mainColor, darkColor, lightColor, borderColor);
			}
		}
		for(int i = 0; i < 4; i++) {
			generateVariationTile(pixels, textureWidth, i * tileSize, textureSize, tileSize, i, variationColor, darkColor);
		}
		tileset.SetData(pixels);
		return tileset;
	}

	private static void generateVariationTile(Color[] pixels, int textureWidth, int startX, int startY,
				int tileSize, int variationIndex, Color detailColor, Color darkColor) {
		// Create sparse random details (mostly transparent)
		for(int y = 0; y < tileSize; y++) {
			for(int x = 0; x < tileSize; x++) {
				int pixelX = startX + x;
				int pixelY = startY + y;
				int index = pixelY * textureWidth + pixelX;

				// Use hash for pseudo-random but consistent pattern
				int hash = (x * 374761393 + y * 668265263 + variationIndex * 1000) % 100;

				if(hash < 8) {
					pixels[index] = detailColor * 0.7f;
				} else if(hash < 15){
					pixels[index] = darkColor * 0.5f;
				} else {
					pixels[index] = Color.Transparent;
				}
			}
		}
	}

	private static Color getVariationColor(TileType terrainType) {
		return terrainType switch {
			TileType.Grass => new Color(40, 160, 40),   // Darker grass specs
			TileType.Water => new Color(30, 130, 220),  // Water sparkles
			TileType.Stone => new Color(120, 120, 120), // Stone specs
			TileType.Tree => new Color(15, 100, 15),    // Dark tree details
			_ => Color.Gray
		};
	}

	private static void generateTile(Color[] pixels, int textureWidth, int startX, int startY, int tileSize, int mask,
				Color mainColor, Color darkColor, Color lightColor, Color borderColor) {
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

				if(inTopHalf && inLeftHalf){
					shouldFill = topLeft;
				} else if(inTopHalf && !inLeftHalf){
					shouldFill = topRight;
				} else if(!inTopHalf && inLeftHalf){
					shouldFill = bottomLeft;
				} else{
					shouldFill = bottomRight;
				}

				if(shouldFill) {
					// Add some texture/pattern
					Color pixelColor = getTexturedPixel(x, y, tileSize, mainColor, darkColor, lightColor);
					pixels[index] = pixelColor;
				} else {
					// Transparent or background
					pixels[index] = Color.Transparent;
				}

				// Add border on edges between filled and empty
				if(shouldDrawBorder(x, y, halfSize, topLeft, topRight, bottomLeft, bottomRight)) {
					pixels[index] = borderColor;
				}
			}
		}
	}

	private static bool shouldDrawBorder(int x, int y, int halfSize, bool tl, bool tr, bool bl, bool br) {
		bool inTopHalf = y < halfSize;
		bool inLeftHalf = x < halfSize;
		bool onHorizontalEdge = y == halfSize - 1 || y == halfSize;
		bool onVerticalEdge = x == halfSize - 1 || x == halfSize;

		// Draw border where filled meets empty
		if(onHorizontalEdge && inLeftHalf && tl != bl) {
			return true;
		}
		if(onHorizontalEdge && !inLeftHalf && tr != br) {
			return true;
		}
		if(onVerticalEdge && inTopHalf && tl != tr) {
			return true;
		}
		if(onVerticalEdge && !inTopHalf && bl != br) {
			return true;
		}

		return false;
	}

	private static Color getTexturedPixel(int x, int y, int tileSize, Color main, Color dark, Color light) {
		// Simple noise-like pattern
		int hash = (x * 374761393 + y * 668265263) % 100;
		if(hash < 10) {
			return dark;
		} else if(hash < 20) {
			return light;
		} else{
			return main;
		}
	}

	private static Color getMainColor(TileType terrainType) {
		return terrainType switch {
			TileType.Grass => new Color(50, 180, 50),
			TileType.Water => new Color(40, 150, 255),
			TileType.Stone => new Color(140, 140, 140),
			TileType.Tree => new Color(20, 120, 20),
			_ => Color.White
		};
	}

	private static Color getDarkColor(TileType terrainType) {
		return terrainType switch {
			TileType.Grass => new Color(30, 140, 30),
			TileType.Water => new Color(20, 100, 200),
			TileType.Stone => new Color(100, 100, 100),
			TileType.Tree => new Color(10, 80, 10),
			_ => Color.Gray
		};
	}

	private static Color getLightColor(TileType terrainType) {
		return terrainType switch {
			TileType.Grass => new Color(70, 220, 70),
			TileType.Water => new Color(60, 180, 255),
			TileType.Stone => new Color(180, 180, 180),
			TileType.Tree => new Color(30, 160, 30),
			_ => Color.LightGray
		};
	}

	private static Color getBorderColor(TileType terrainType) {
		return terrainType switch {
			TileType.Grass => new Color(20, 100, 20),
			TileType.Water => new Color(10, 80, 180),
			TileType.Stone => new Color(80, 80, 80),
			TileType.Tree => new Color(5, 60, 5),
			_ => Color.Black
		};
	}
}