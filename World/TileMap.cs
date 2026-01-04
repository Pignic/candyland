using EldmoresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.World;

public class TileMap {

	public const string DEFAULT_TILE = "grass";

	public int Width { get; }
	public int Height { get; }
	public int TileSize { get; }

	private string[,] worldGrid;

	private Dictionary<string, Texture2D> tilesets;

	private Texture2D pixelTexture;

	public int PixelWidth => Width * TileSize;
	public int PixelHeight => Height * TileSize;

	// Shader config
	private Effect variationMaskEffect;
	private EffectParameter tileSizeParam;
	private EffectParameter textureSizeParam;

	public void LoadVariationShader(Effect effect) {
		variationMaskEffect = effect;
		tileSizeParam = variationMaskEffect?.Parameters["TileSize"];
		textureSizeParam = variationMaskEffect?.Parameters["TextureSize"];
	}

	public TileMap(int width, int height, int tileSize, GraphicsDevice graphicsDevice, int? seed = null) {
		Width = width;
		Height = height;
		TileSize = tileSize;

		worldGrid = new string[width, height];
		tilesets = new Dictionary<string, Texture2D>();

		// Create 1x1 white texture for fallback
		pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		pixelTexture.SetData([Color.White]);

		if (seed != null) {
			GenerateMap(seed.Value);
		}
	}

	public void LoadTileset(string terrainType, Texture2D tileset) {
		tilesets[terrainType] = tileset;
	}

	private void GenerateMap(int seed) {
		Random random = new Random(seed);

		// Fill with grass
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				worldGrid[x, y] = DEFAULT_TILE;
			}
		}

		// Add water patches
		for (int i = 0; i < 5; i++) {
			int centerX = random.Next(2, Width - 2);
			int centerY = random.Next(2, Height - 2);
			int size = random.Next(2, 5);

			for (int x = -size; x <= size; x++) {
				for (int y = -size; y <= size; y++) {
					int tileX = centerX + x;
					int tileY = centerY + y;

					if (tileX >= 0 && tileX < Width && tileY >= 0 && tileY < Height) {
						if ((x * x) + (y * y) <= size * size) {
							worldGrid[tileX, tileY] = "water";
						}
					}
				}
			}
		}

		// Add trees
		for (int i = 0; i < 20; i++) {
			int x = random.Next(0, Width);
			int y = random.Next(0, Height);

			if (worldGrid[x, y] == DEFAULT_TILE) {
				worldGrid[x, y] = "tree";
			}
		}

		// Add stone border
		for (int x = 0; x < Width; x++) {
			worldGrid[x, 0] = "stone";
			worldGrid[x, Height - 1] = "stone";
		}
		for (int y = 0; y < Height; y++) {
			worldGrid[0, y] = "stone";
			worldGrid[Width - 1, y] = "stone";
		}
	}

	public void Draw(SpriteBatch spriteBatch, Rectangle visibleArea, Matrix cameraTransform) {
		int halfTile = TileSize / 2;
		int startX = Math.Max(0, (visibleArea.X - halfTile) / TileSize);
		int endX = Math.Min(Width, ((visibleArea.Right - halfTile) / TileSize) + 1);
		int startY = Math.Max(0, (visibleArea.Y - halfTile) / TileSize);
		int endY = Math.Min(Height, ((visibleArea.Bottom - halfTile) / TileSize) + 1);

		string[] drawOrder = TileRegistry.Instance.GetAllTiles().OrderBy(t => t.DrawOrder).Select(t => t.Id).ToArray();

		if (variationMaskEffect != null) {
			// PASS 1: Draw all tiles with shader (batch them together)
			spriteBatch.End();

			tileSizeParam?.SetValue((float)TileSize);
			foreach (string terrainType in drawOrder) {
				if (!tilesets.ContainsKey(terrainType)) {
					continue;
				}

				Texture2D tileset = tilesets[terrainType];

				// Start batch with shader for this terrain type
				spriteBatch.Begin(
					blendState: BlendState.AlphaBlend,
					samplerState: SamplerState.PointClamp,
					effect: variationMaskEffect,
					transformMatrix: cameraTransform
				);

				// Set texture size once per terrain
				textureSizeParam.SetValue(new Vector2(tileset.Width, tileset.Height));

				for (int x = startX; x < endX; x++) {
					for (int y = startY; y < endY; y++) {
						DrawDisplayTileWithShader(spriteBatch, tileset, x, y, terrainType);
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
			foreach (string terrainType in drawOrder) {
				for (int x = startX; x < endX; x++) {
					for (int y = startY; y < endY; y++) {
						DrawDisplayTile(spriteBatch, x, y, terrainType);
					}
				}
			}
		}
	}

	private void DrawDisplayTile(SpriteBatch spriteBatch, int displayX, int displayY, string terrainType) {
		int mask = GetMask(displayX, displayY, terrainType);
		if (mask == 0) {
			return;
		}

		int halfTile = TileSize / 2;
		Rectangle destRect = new Rectangle(
			(displayX * TileSize) + halfTile,
			(displayY * TileSize) + halfTile,
			TileSize,
			TileSize
		);

		if (tilesets.ContainsKey(terrainType)) {
			Rectangle sourceRect = GetTileSourceRect(mask, TileSize);
			spriteBatch.Draw(tilesets[terrainType], destRect, sourceRect, Color.White);
			spriteBatch.Draw(tilesets[terrainType], destRect, null, Color.White);
		} else {
			Color color = GetTileColor(terrainType);
			spriteBatch.Draw(pixelTexture, destRect, color);
		}
	}

	private void DrawDisplayTileWithShader(SpriteBatch spriteBatch, Texture2D tileset, int displayX, int displayY, string terrainType) {
		int mask = GetMask(displayX, displayY, terrainType);
		if (mask == 0) {
			return;
		}

		int halfTile = TileSize / 2;
		Rectangle destRect = new Rectangle(
			(displayX * TileSize) + halfTile,
			(displayY * TileSize) + halfTile,
			TileSize,
			TileSize
		);

		Rectangle sourceRect = GetTileSourceRect(mask, TileSize);

		// Encode all needed data in Color:
		int tileX = displayX % 16;  // For variation (0-15)
		int tileY = displayY % 16;  // For variation (0-15)
		int sourceCol = sourceRect.X / TileSize;  // Which column (0-3)
		int sourceRow = sourceRect.Y / TileSize;  // Which row (0-3)

		Color tileInfo = new Color(
			tileX * 16 / 255f,      // R: tile X for variation
			tileY * 16 / 255f,      // G: tile Y for variation
			sourceCol * 64 / 255f,  // B: source column (0, 64, 128, 192)
			sourceRow * 64 / 255f   // A: source row (0, 64, 128, 192)
		);

		spriteBatch.Draw(tileset, destRect, sourceRect, tileInfo);
	}

	private int GetMask(int displayX, int displayY, string terrainType) {
		string topLeft = GetWorldTile(displayX, displayY);
		string topRight = GetWorldTile(displayX + 1, displayY);
		string bottomLeft = GetWorldTile(displayX, displayY + 1);
		string bottomRight = GetWorldTile(displayX + 1, displayY + 1);

		int mask = 0;
		if (topLeft == terrainType) {
			mask |= 8;
		}
		if (topRight == terrainType) {
			mask |= 4;
		}
		if (bottomLeft == terrainType) {
			mask |= 2;
		}
		if (bottomRight == terrainType) {
			mask |= 1;
		}
		return mask;
	}

	// Lookup table: mask -> (column, row)
	private static readonly int[] columnLookup = [2, 3, 2, 1, 2, 3, 0, 3, 1, 2, 1, 0, 3, 0, 1, 0];
	private static readonly int[] rowLookup = [1, 1, 2, 2, 0, 2, 1, 3, 1, 3, 0, 2, 0, 0, 3, 3];

	private Rectangle GetTileSourceRect(int mask, int tileSize) {
		// [13][10][ 4][12]  Row 0
		// [ 6][ 8][ 0][ 1]  Row 1
		// [11][ 3][ 2][ 5]  Row 2
		// [15][14][ 9][ 7]  Row 3
		return new Rectangle(
			columnLookup[mask] * tileSize,
			rowLookup[mask] * tileSize,
			tileSize,
			tileSize
		);
	}

	private string GetWorldTile(int x, int y) {
		if (x < 0 || x >= Width || y < 0 || y >= Height) {
			return DEFAULT_TILE; // Default to grass outside bounds
		}
		return worldGrid[x, y];
	}

	public string GetTileAtPosition(Vector2 position) {
		int x = (int)(position.X / TileSize);
		int y = (int)(position.Y / TileSize);
		return GetWorldTile(x, y);
	}

	public void SetTile(int x, int y, string tileType) {
		if (x < 0 || x >= Width || y < 0 || y >= Height) {
			return;
		}
		worldGrid[x, y] = tileType;
	}

	public void SetTile(int x, int y, int tileNum) {
		SetTile(x, y, TileRegistry.Instance.GetAllTiles().ToArray()[tileNum].Id);
	}

	// Get tile at grid coordinates
	public string GetTile(int x, int y) {
		if (x < 0 || x >= Width || y < 0 || y >= Height) {
			return null;
		}
		return worldGrid[x, y];
	}

	public bool CheckCollision(Rectangle bounds) {
		// Check all four corners and center
		Vector2[] checkPoints = [
			new Vector2(bounds.Left, bounds.Top),
			new Vector2(bounds.Right - 1, bounds.Top),
			new Vector2(bounds.Left, bounds.Bottom - 1),
			new Vector2(bounds.Right - 1, bounds.Bottom - 1),
			new Vector2(bounds.Center.X, bounds.Center.Y)
		];

		foreach (Vector2 point in checkPoints) {
			string tileType = GetTileAtPosition(point);
			if (!IsWalkable(tileType)) {
				return true;
			}
		}
		return false;
	}

	private bool IsWalkable(string tileType) {
		TileDefinition definition = TileRegistry.Instance.GetTile(tileType);
		return definition?.IsWalkable ?? true;
	}

	private Color GetTileColor(string tileType) {
		TileDefinition definition = TileRegistry.Instance.GetTile(tileType);
		return definition?.MainColor ?? Color.White;
	}
}