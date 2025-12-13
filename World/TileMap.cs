using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Candyland.World {

	public class TileMap {

		public int Width { get; private set; }
		public int Height { get; private set; }
		public int TileSize { get; private set; }

		private TileType[,] _worldGrid;

		private Dictionary<TileType, Texture2D> _tilesets;

		private Texture2D _pixelTexture;

		public int PixelWidth => Width * TileSize;
		public int PixelHeight => Height * TileSize;

		private Effect _variationMaskEffect;
		public void LoadVariationShader(Effect effect) {
			_variationMaskEffect = effect;
		}

		public TileMap(int width, int height, int tileSize, GraphicsDevice graphicsDevice, int seed = 42) {
			Width = width;
			Height = height;
			TileSize = tileSize;

			_worldGrid = new TileType[width, height];
			_tilesets = new Dictionary<TileType, Texture2D>();

			// Create 1x1 white texture for fallback
			_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			_pixelTexture.SetData(new[] { Color.White });

			GenerateMap(seed);
			
		}

		public void LoadTileset(TileType terrainType, Texture2D tileset) {
			_tilesets[terrainType] = tileset;
		}

		private void GenerateMap(int seed) {
			var random = new Random(seed);

			// Fill with grass
			for(int x = 0; x < Width; x++) {
				for(int y = 0; y < Height; y++) {
					_worldGrid[x, y] = TileType.Grass;
				}
			}

			// Add water patches
			for(int i = 0; i < 5; i++) {
				int centerX = random.Next(2, Width - 2);
				int centerY = random.Next(2, Height - 2);
				int size = random.Next(2, 5);

				for(int x = -size; x <= size; x++) {
					for(int y = -size; y <= size; y++) {
						int tileX = centerX + x;
						int tileY = centerY + y;

						if(tileX >= 0 && tileX < Width && tileY >= 0 && tileY < Height) {
							if(x * x + y * y <= size * size) {
								_worldGrid[tileX, tileY] = TileType.Water;
							}
						}
					}
				}
			}

			// Add trees
			for(int i = 0; i < 20; i++) {
				int x = random.Next(0, Width);
				int y = random.Next(0, Height);

				if(_worldGrid[x, y] == TileType.Grass) {
					_worldGrid[x, y] = TileType.Tree;
				}
			}

			// Add stone border
			for(int x = 0; x < Width; x++) {
				_worldGrid[x, 0] = TileType.Stone;
				_worldGrid[x, Height - 1] = TileType.Stone;
			}
			for(int y = 0; y < Height; y++) {
				_worldGrid[0, y] = TileType.Stone;
				_worldGrid[Width - 1, y] = TileType.Stone;
			}
		}

		public void Draw(SpriteBatch spriteBatch, Rectangle visibleArea, Matrix cameraTransform) {
			int halfTile = TileSize / 2;
			int startX = Math.Max(0, (visibleArea.X - halfTile) / TileSize);
			int endX = Math.Min(Width, (visibleArea.Right - halfTile) / TileSize + 1);
			int startY = Math.Max(0, (visibleArea.Y - halfTile) / TileSize);
			int endY = Math.Min(Height, (visibleArea.Bottom - halfTile) / TileSize + 1);

			TileType[] drawOrder = { TileType.Water, TileType.Grass, TileType.Stone, TileType.Tree };

			if(_variationMaskEffect != null) {
				// PASS 1: Draw all tiles with shader (batch them together)
				spriteBatch.End();

				foreach(var terrainType in drawOrder) {
					if(!_tilesets.ContainsKey(terrainType)) continue;

					var tileset = _tilesets[terrainType];

					var matrixParam = _variationMaskEffect.Parameters["MatrixTransform"];
					if(matrixParam != null) {
						//matrixParam.SetValue(cameraTransform);
					}

					// Start batch with shader for this terrain type
					spriteBatch.Begin(
						samplerState: SamplerState.PointClamp,
						effect: _variationMaskEffect, 
						blendState: BlendState.AlphaBlend
						, transformMatrix: cameraTransform
					);

					// Set texture size once per terrain
					EffectParameter param = _variationMaskEffect.Parameters["TextureSize"];
					if(param != null) {
						param.SetValue(new Vector2(tileset.Width, tileset.Height));
					}

					for(int x = startX; x < endX; x++) {
						for(int y = startY; y < endY; y++) {
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
				foreach(var terrainType in drawOrder) {
					for(int x = startX; x < endX; x++) {
						for(int y = startY; y < endY; y++) {
							DrawDisplayTile(spriteBatch, x, y, terrainType);
						}
					}
				}
			}
		}

		private void DrawDisplayTile(SpriteBatch spriteBatch, int displayX, int displayY, TileType terrainType) {
			TileType topLeft = GetWorldTile(displayX, displayY);
			TileType topRight = GetWorldTile(displayX + 1, displayY);
			TileType bottomLeft = GetWorldTile(displayX, displayY + 1);
			TileType bottomRight = GetWorldTile(displayX + 1, displayY + 1);

			int mask = 0;
			if(topLeft == terrainType) mask |= 8;
			if(topRight == terrainType) mask |= 4;
			if(bottomLeft == terrainType) mask |= 2;
			if(bottomRight == terrainType) mask |= 1;

			if(mask == 0) return;

			int halfTile = TileSize / 2;
			Rectangle destRect = new Rectangle(
				displayX * TileSize + halfTile,
				displayY * TileSize + halfTile,
				TileSize,
				TileSize
			);

			if(_tilesets.ContainsKey(terrainType)) {
				Rectangle sourceRect = GetTileSourceRect(mask, TileSize);
				int variationIndex = ((displayX * 7 + displayY * 13) % 4);
				Rectangle variationSourceRect = new Rectangle(
					variationIndex * TileSize,
					TileSize * 4,
					TileSize,
					TileSize
				);

				spriteBatch.Draw(_tilesets[terrainType], destRect, sourceRect, Color.White);
				spriteBatch.Draw(_tilesets[terrainType], destRect, variationSourceRect, Color.White);
			} else {
				Color color = GetTileColor(terrainType);
				spriteBatch.Draw(_pixelTexture, destRect, color);
			}
		}

		private void DrawDisplayTileWithShader(SpriteBatch spriteBatch, Texture2D tileset, int displayX, int displayY, TileType terrainType) {
			TileType topLeft = GetWorldTile(displayX, displayY);
			TileType topRight = GetWorldTile(displayX + 1, displayY);
			TileType bottomLeft = GetWorldTile(displayX, displayY + 1);
			TileType bottomRight = GetWorldTile(displayX + 1, displayY + 1);

			int mask = 0;
			if(topLeft == terrainType) mask |= 8;
			if(topRight == terrainType) mask |= 4;
			if(bottomLeft == terrainType) mask |= 2;
			if(bottomRight == terrainType) mask |= 1;

			if(mask == 0) return;

			int halfTile = TileSize / 2;
			Rectangle destRect = new Rectangle(
				displayX * TileSize + halfTile,
				displayY * TileSize + halfTile,
				TileSize,
				TileSize
			);

			Rectangle sourceRect = GetTileSourceRect(mask, TileSize);
			int variationIndex = ((displayX * 7 + displayY * 13) % 4);
			Rectangle variationSourceRect = new Rectangle(
				variationIndex * TileSize,
				TileSize * 4,
				TileSize,
				TileSize
			);

			// Set shader parameters for THIS tile
			EffectParameter baseSourceRectParam = _variationMaskEffect.Parameters["BaseSourceRect"];
			if(baseSourceRectParam != null) {
				baseSourceRectParam.SetValue(
					new Vector4(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height)
				);
			}


			EffectParameter variationSourceRectParam = _variationMaskEffect.Parameters["VariationSourceRect"];
			if(variationSourceRectParam != null) {
				variationSourceRectParam.SetValue(
					new Vector4(variationSourceRect.X, variationSourceRect.Y,
							   variationSourceRect.Width, variationSourceRect.Height)
				);
			}
			//_variationMaskEffect.Parameters["TextureSampler"].SetValue(tileset);

			// Draw (shader is already active from Begin())
			spriteBatch.Draw(tileset, destRect, sourceRect, Color.White);
		}

		/// <summary>
		/// Get source rectangle in tileset for a given bitmask
		/// Tileset layout: 4x4 grid, tiles numbered 0-15
		/// </summary>
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

		private TileType GetWorldTile(int x, int y) {
			if(x < 0 || x >= Width || y < 0 || y >= Height)
				return TileType.Grass; // Default to grass outside bounds

			return _worldGrid[x, y];
		}

		public TileType GetTileAtPosition(Vector2 position) {
			int x = (int)(position.X / TileSize);
			int y = (int)(position.Y / TileSize);

			return GetWorldTile(x, y);
		}

		public void SetTile(int x, int y, TileType tileType) {
			if(x < 0 || x >= Width || y < 0 || y >= Height)
				return;

			_worldGrid[x, y] = tileType;
		}

		// Get tile at grid coordinates
		public TileType? getTile(int x, int y) {
			if(x < 0 || x >= Width || y < 0 || y >= Height)
				return null;
			return _worldGrid[x, y];
		}

		public bool CheckCollision(Rectangle bounds) {
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
				var tileType = GetTileAtPosition(point);
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

		// Check if a rectangle collides with unwalkable tiles
		public bool checkCollision(Rectangle bounds) {
			// Check all four corners and center of the entity
			Vector2[] checkPoints = new Vector2[] {
				new Vector2(bounds.Left, bounds.Top),
				new Vector2(bounds.Right - 1, bounds.Top),
				new Vector2(bounds.Left, bounds.Bottom - 1),
				new Vector2(bounds.Right - 1, bounds.Bottom - 1),
				new Vector2(bounds.Center.X, bounds.Center.Y)
			};

			foreach(var point in checkPoints) {
				var tile = GetTileAtPosition(point);
				if(!IsWalkable(tile)) {
					return true;
				}
			}
			return false;
		}
	}
}