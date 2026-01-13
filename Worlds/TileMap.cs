using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.Worlds;

public class TileMap {

	public struct MovementResult {
		public Vector2 Movement;
		public Vector2 BlockedVelocity;
		public readonly bool WasBlocked => BlockedVelocity != Vector2.Zero;
		public Vector2 CollisionNormal;

		public MovementResult(Vector2 movement, Vector2 blockedVelocity, Vector2 normal) {
			Movement = movement;
			BlockedVelocity = blockedVelocity;
			CollisionNormal = normal;
		}
	}

	public const string DEFAULT_TILE = "grass";

	public int Width { get; }
	public int Height { get; }
	public int TileSize { get; }
	public int PixelWidth => Width * TileSize;
	public int PixelHeight => Height * TileSize;

	private readonly string[,] _worldGrid;

	private readonly Dictionary<string, Texture2D> _tilesets;

	// TODO get that from the assetLoader
	private readonly Texture2D _pixelTexture;

	// Shader config
	private Effect _variationMaskEffect;
	private EffectParameter _tileSizeParam;
	private EffectParameter _textureSizeParam;

	public void LoadVariationShader(Effect effect) {
		_variationMaskEffect = effect;
		_tileSizeParam = _variationMaskEffect?.Parameters["TileSize"];
		_textureSizeParam = _variationMaskEffect?.Parameters["TextureSize"];
	}

	public TileMap(int width, int height, int tileSize, GraphicsDevice graphicsDevice, int? seed = null) {
		Width = width;
		Height = height;
		TileSize = tileSize;

		_worldGrid = new string[width, height];
		_tilesets = [];

		// Create 1x1 white texture for fallback
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData([Color.White]);

		if (seed != null) {
			GenerateMap(seed.Value);
		}
	}

	public void LoadTileset(string terrainType, Texture2D tileset) {
		_tilesets[terrainType] = tileset;
	}

	private void GenerateMap(int seed) {
		Random random = new Random(seed);

		// Fill with grass
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				_worldGrid[x, y] = DEFAULT_TILE;
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
							_worldGrid[tileX, tileY] = "water";
						}
					}
				}
			}
		}

		// Add trees
		for (int i = 0; i < 20; i++) {
			int x = random.Next(0, Width);
			int y = random.Next(0, Height);

			if (_worldGrid[x, y] == DEFAULT_TILE) {
				_worldGrid[x, y] = "tree";
			}
		}

		// Add stone border
		for (int x = 0; x < Width; x++) {
			_worldGrid[x, 0] = "stone";
			_worldGrid[x, Height - 1] = "stone";
		}
		for (int y = 0; y < Height; y++) {
			_worldGrid[0, y] = "stone";
			_worldGrid[Width - 1, y] = "stone";
		}
	}

	public void Draw(SpriteBatch spriteBatch, Rectangle visibleArea, Matrix cameraTransform) {
		int halfTile = TileSize / 2;
		int startX = Math.Max(0, (visibleArea.X - halfTile) / TileSize);
		int endX = Math.Min(Width, ((visibleArea.Right - halfTile) / TileSize) + 1);
		int startY = Math.Max(0, (visibleArea.Y - halfTile) / TileSize);
		int endY = Math.Min(Height, ((visibleArea.Bottom - halfTile) / TileSize) + 1);

		string[] drawOrder = TileRegistry.Instance.GetAllTiles().OrderBy(t => t.DrawOrder).Select(t => t.Id).ToArray();

		if (_variationMaskEffect != null) {
			// PASS 1: Draw all tiles with shader (batch them together)
			spriteBatch.End();

			_tileSizeParam?.SetValue((float)TileSize);
			foreach (string terrainType in drawOrder) {
				if (!_tilesets.ContainsKey(terrainType)) {
					continue;
				}

				Texture2D tileset = _tilesets[terrainType];

				// Start batch with shader for this terrain type
				spriteBatch.Begin(
					blendState: BlendState.AlphaBlend,
					samplerState: SamplerState.PointClamp,
					effect: _variationMaskEffect,
					transformMatrix: cameraTransform
				);

				// Set texture size once per terrain
				_textureSizeParam.SetValue(new Vector2(tileset.Width, tileset.Height));

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

		if (_tilesets.TryGetValue(terrainType, out Texture2D value)) {
			Rectangle sourceRect = GetTileSourceRect(mask, TileSize);
			spriteBatch.Draw(value, destRect, sourceRect, Color.White);
			spriteBatch.Draw(value, destRect, null, Color.White);
		} else {
			Color color = GetTileColor(terrainType);
			spriteBatch.Draw(_pixelTexture, destRect, color);
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

	private static Rectangle GetTileSourceRect(int mask, int tileSize) =>
		// [13][10][ 4][12]  Row 0
		// [ 6][ 8][ 0][ 1]  Row 1
		// [11][ 3][ 2][ 5]  Row 2
		// [15][14][ 9][ 7]  Row 3
		new Rectangle(
			columnLookup[mask] * tileSize,
			rowLookup[mask] * tileSize,
			tileSize,
			tileSize
		);

	private string GetWorldTile(int x, int y) {
		if (x < 0 || x >= Width || y < 0 || y >= Height) {
			return DEFAULT_TILE; // Default to grass outside bounds
		}
		return _worldGrid[x, y];
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
		_worldGrid[x, y] = tileType;
	}

	public void SetTile(int x, int y, int tileNum) {
		SetTile(x, y, TileRegistry.Instance.GetAllTiles().ToArray()[tileNum].Id);
	}

	// Get tile at grid coordinates
	public string GetTile(int x, int y) {
		if (x < 0 || x >= Width || y < 0 || y >= Height) {
			return null;
		}
		return _worldGrid[x, y];
	}

	public bool IsRectangleWalkable(Rectangle bounds) {

		int startX = bounds.Left / TileSize;
		int endX = (bounds.Right - 1) / TileSize;
		int startY = bounds.Top / TileSize;
		int endY = (bounds.Bottom - 1) / TileSize;

		// Clamp to valid range
		startX = Math.Max(0, startX);
		endX = Math.Min(Width - 1, endX);
		startY = Math.Max(0, startY);
		endY = Math.Min(Height - 1, endY);

		for (int x = startX; x <= endX; x++) {
			for (int y = startY; y <= endY; y++) {
				if (!IsWalkable(GetWorldTile(x, y))) {
					return false;
				}
			}
		}
		return true;
	}

	public MovementResult ResolveMovement(Rectangle bounds, Vector2 desiredMovement) {
		if (desiredMovement == Vector2.Zero) {
			return new MovementResult(Vector2.Zero, Vector2.Zero, Vector2.Zero);
		}

		Vector2 actualMovement = desiredMovement;
		Vector2 blockedVelocity = Vector2.Zero;
		Vector2 normal = Vector2.Zero;

		// Try horizontal movement
		Rectangle horizontalBounds = bounds;
		horizontalBounds.X += (int)desiredMovement.X;

		if (!IsRectangleWalkable(horizontalBounds)) {
			blockedVelocity.X = desiredMovement.X;
			actualMovement.X = 0;
			normal.X = desiredMovement.X > 0 ? -1 : 1; // Wall is to the left or right
		}

		// Try vertical movement
		Rectangle verticalBounds = bounds;
		verticalBounds.Y += (int)desiredMovement.Y;

		if (!IsRectangleWalkable(verticalBounds)) {
			blockedVelocity.Y = desiredMovement.Y;
			actualMovement.Y = 0;
			normal.Y = desiredMovement.Y > 0 ? -1 : 1; // Wall is above or below
		}

		return new MovementResult(actualMovement, blockedVelocity, normal);
	}

	public bool IsTileWalkable(int tileX, int tileY) {
		string tileId = GetWorldTile(tileX, tileY);
		return IsWalkable(tileId);
	}

	private static bool IsWalkable(string tileType) {
		TileDefinition definition = TileRegistry.Instance.GetTile(tileType);
		return definition?.IsWalkable ?? true;
	}

	private static Color GetTileColor(string tileType) {
		TileDefinition definition = TileRegistry.Instance.GetTile(tileType);
		return definition?.MainColor ?? Color.White;
	}
}
