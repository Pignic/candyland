using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Candyland.World;

public class TileMap {

	public int width { get; private set; }
	public int height { get; private set; }
	public int tileSize { get; private set; }

	private TileType[,] worldGrid;

	private Dictionary<TileType, Texture2D> tilesets;

	private Texture2D pixelTexture;

	public int pixelWidth => width * tileSize;
	public int pixelHeight => height * tileSize;

	// Shader config
	private Effect variationMaskEffect;
	private EffectParameter tileSizeParam;
	private EffectParameter textureSizeParam;

	public void loadVariationShader(Effect effect) {
		variationMaskEffect = effect;
		tileSizeParam = variationMaskEffect?.Parameters["TileSize"];
		textureSizeParam = variationMaskEffect?.Parameters["TextureSize"];
	}

	public TileMap(int width, int height, int tileSize, GraphicsDevice graphicsDevice, int seed = 42) {
		this.width = width;
		this.height = height;
		this.tileSize = tileSize;

		worldGrid = new TileType[width, height];
		tilesets = new Dictionary<TileType, Texture2D>();

		// Create 1x1 white texture for fallback
		pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		pixelTexture.SetData(new[] { Color.White });

		generateMap(seed);
	}

	public void loadTileset(TileType terrainType, Texture2D tileset) {
		tilesets[terrainType] = tileset;
	}

	private void generateMap(int seed) {
		var random = new Random(seed);

		// Fill with grass
		for(int x = 0; x < width; x++) {
			for(int y = 0; y < height; y++) {
				worldGrid[x, y] = TileType.Grass;
			}
		}

		// Add water patches
		for(int i = 0; i < 5; i++) {
			int centerX = random.Next(2, width - 2);
			int centerY = random.Next(2, height - 2);
			int size = random.Next(2, 5);

			for(int x = -size; x <= size; x++) {
				for(int y = -size; y <= size; y++) {
					int tileX = centerX + x;
					int tileY = centerY + y;

					if(tileX >= 0 && tileX < width && tileY >= 0 && tileY < height) {
						if(x * x + y * y <= size * size) {
							worldGrid[tileX, tileY] = TileType.Water;
						}
					}
				}
			}
		}

		// Add trees
		for(int i = 0; i < 20; i++) {
			int x = random.Next(0, width);
			int y = random.Next(0, height);

			if(worldGrid[x, y] == TileType.Grass) {
				worldGrid[x, y] = TileType.Tree;
			}
		}

		// Add stone border
		for(int x = 0; x < width; x++) {
			worldGrid[x, 0] = TileType.Stone;
			worldGrid[x, height - 1] = TileType.Stone;
		}
		for(int y = 0; y < height; y++) {
			worldGrid[0, y] = TileType.Stone;
			worldGrid[width - 1, y] = TileType.Stone;
		}
	}

	public void draw(SpriteBatch spriteBatch, Rectangle visibleArea, Matrix cameraTransform) {
		int halfTile = tileSize / 2;
		int startX = Math.Max(0, (visibleArea.X - halfTile) / tileSize);
		int endX = Math.Min(width, (visibleArea.Right - halfTile) / tileSize + 1);
		int startY = Math.Max(0, (visibleArea.Y - halfTile) / tileSize);
		int endY = Math.Min(height, (visibleArea.Bottom - halfTile) / tileSize + 1);

		TileType[] drawOrder = { TileType.Water, TileType.Grass, TileType.Stone, TileType.Tree };

		if(variationMaskEffect != null) {
			// PASS 1: Draw all tiles with shader (batch them together)
			spriteBatch.End();

			tileSizeParam?.SetValue((float)tileSize);
			foreach(var terrainType in drawOrder) {
				if(!tilesets.ContainsKey(terrainType)) continue;

				var tileset = tilesets[terrainType];

				// Start batch with shader for this terrain type
				spriteBatch.Begin(
					blendState: BlendState.AlphaBlend
,
					samplerState: SamplerState.PointClamp,
					effect: variationMaskEffect, transformMatrix: cameraTransform
				);

				// Set texture size once per terrain
				textureSizeParam.SetValue(new Vector2(tileset.Width, tileset.Height));

				for(int x = startX; x < endX; x++) {
					for(int y = startY; y < endY; y++) {
						drawDisplayTileWithShader(spriteBatch, tileset, x, y, terrainType);
					}
				}

				spriteBatch.End();
			}

			// Resume normal rendering for everything else (player, UI, etc)
			spriteBatch.Begin(
				samplerState: SamplerState.PointClamp,
				transformMatrix: cameraTransform
			);
		} else {
			// Fallback: no shader
			foreach(var terrainType in drawOrder) {
				for(int x = startX; x < endX; x++) {
					for(int y = startY; y < endY; y++) {
						drawDisplayTile(spriteBatch, x, y, terrainType);
					}
				}
			}
		}
	}

	private void drawDisplayTile(SpriteBatch spriteBatch, int displayX, int displayY, TileType terrainType) {
		TileType topLeft = getWorldTile(displayX, displayY);
		TileType topRight = getWorldTile(displayX + 1, displayY);
		TileType bottomLeft = getWorldTile(displayX, displayY + 1);
		TileType bottomRight = getWorldTile(displayX + 1, displayY + 1);

		int mask = 0;
		if(topLeft == terrainType) mask |= 8;
		if(topRight == terrainType) mask |= 4;
		if(bottomLeft == terrainType) mask |= 2;
		if(bottomRight == terrainType) mask |= 1;

		if(mask == 0) return;

		int halfTile = tileSize / 2;
		Rectangle destRect = new Rectangle(
			displayX * tileSize + halfTile,
			displayY * tileSize + halfTile,
			tileSize,
			tileSize
		);

		if(tilesets.ContainsKey(terrainType)) {
			Rectangle sourceRect = GetTileSourceRect(mask, tileSize);

			spriteBatch.Draw(tilesets[terrainType], destRect, sourceRect, Color.White);
			spriteBatch.Draw(tilesets[terrainType], destRect, null, Color.White);
		} else {
			Color color = GetTileColor(terrainType);
			spriteBatch.Draw(pixelTexture, destRect, color);
		}
	}

	private void drawDisplayTileWithShader(SpriteBatch spriteBatch, Texture2D tileset, int displayX, int displayY, TileType terrainType) {
		TileType topLeft = getWorldTile(displayX, displayY);
		TileType topRight = getWorldTile(displayX + 1, displayY);
		TileType bottomLeft = getWorldTile(displayX, displayY + 1);
		TileType bottomRight = getWorldTile(displayX + 1, displayY + 1);

		int mask = 0;
		if(topLeft == terrainType) mask |= 8;
		if(topRight == terrainType) mask |= 4;
		if(bottomLeft == terrainType) mask |= 2;
		if(bottomRight == terrainType) mask |= 1;

		if(mask == 0) return;

		int halfTile = tileSize / 2;
		Rectangle destRect = new Rectangle(
			displayX * tileSize + halfTile,
			displayY * tileSize + halfTile,
			tileSize,
			tileSize
		);

		Rectangle sourceRect = GetTileSourceRect(mask, tileSize);

		// Encode all needed data in Color:
		int tileX = displayX % 16;  // For variation (0-15)
		int tileY = displayY % 16;  // For variation (0-15)
		int sourceCol = sourceRect.X / tileSize;  // Which column (0-3)
		int sourceRow = sourceRect.Y / tileSize;  // Which row (0-3)

		Color tileInfo = new Color(
			(tileX * 16) / 255f,      // R: tile X for variation
			(tileY * 16) / 255f,      // G: tile Y for variation
			(sourceCol * 64) / 255f,  // B: source column (0, 64, 128, 192)
			(sourceRow * 64) / 255f   // A: source row (0, 64, 128, 192)
		);

		spriteBatch.Draw(tileset, destRect, sourceRect, tileInfo);
	}

	private Rectangle GetTileSourceRect(int mask, int tileSize) {
		// [13][10][ 4][12]  Row 0
		// [ 6][ 8][ 0][ 1]  Row 1
		// [11][ 3][ 2][ 5]  Row 2
		// [15][14][ 9][ 7]  Row 3

		// Lookup table: mask -> (column, row)
		int[] columnLookup = { 2, 3, 2, 1, 2, 3, 0, 3, 1, 2, 1, 0, 3, 0, 1, 0 };
		int[] rowLookup = { 1, 1, 2, 2, 0, 2, 1, 3, 1, 3, 0, 2, 0, 0, 3, 3 };

		int column = columnLookup[mask];
		int row = rowLookup[mask];

		return new Rectangle(
			column * tileSize,
			row * tileSize,
			tileSize,
			tileSize
		);
	}

	private TileType getWorldTile(int x, int y) {
		if(x < 0 || x >= width || y < 0 || y >= height)
			return TileType.Grass; // Default to grass outside bounds

		return worldGrid[x, y];
	}

	public TileType getTileAtPosition(Vector2 position) {
		int x = (int)(position.X / tileSize);
		int y = (int)(position.Y / tileSize);

		return getWorldTile(x, y);
	}

	public void setTile(int x, int y, TileType tileType) {
		if(x < 0 || x >= width || y < 0 || y >= height)
			return;

		worldGrid[x, y] = tileType;
	}

	// Get tile at grid coordinates
	public TileType? getTile(int x, int y) {
		if(x < 0 || x >= width || y < 0 || y >= height)
			return null;
		return worldGrid[x, y];
	}

	public bool checkCollision(Rectangle bounds) {
		// Check all four corners and center
		Vector2[] checkPoints = new Vector2[]
		{
			new Vector2(bounds.Left, bounds.Top),
			new Vector2(bounds.Right - 1, bounds.Top),
			new Vector2(bounds.Left, bounds.Bottom - 1),
			new Vector2(bounds.Right - 1, bounds.Bottom - 1),
			new Vector2(bounds.Center.X, bounds.Center.Y)
		};

		foreach(var point in checkPoints) {
			var tileType = getTileAtPosition(point);
			if(!IsWalkable(tileType)) {
				return true;
			}
		}

		return false;
	}

	private bool IsWalkable(TileType tileType) {
		return tileType switch {
			TileType.Water => false,
			TileType.Tree => false,
			TileType.Stone => true,
			TileType.Grass => true,
			_ => true
		};
	}

	private Color GetTileColor(TileType tileType) {
		return tileType switch {
			TileType.Grass => new Color(34, 139, 34),
			TileType.Water => new Color(30, 144, 255),
			TileType.Stone => new Color(128, 128, 128),
			TileType.Tree => new Color(0, 100, 0),
			_ => Color.White
		};
	}
}