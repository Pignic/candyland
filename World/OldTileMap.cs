using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.World;

public class OldTileMap {
	public int width { get; private set; }
	public int height { get; private set; }
	public int tileSize { get; private set; }

	private Tile[,] tiles;
	private Texture2D tileTexture;
	public int pixelWidth => width * tileSize;
	public int pixelHeight => height * tileSize;

	public OldTileMap(int width, int height, int tileSize, GraphicsDevice graphicsDevice, int seed = 42) {
		this.width = width;
		this.height = height;
		this.tileSize = tileSize;
		tiles = new Tile[width, height];
		tileTexture = new Texture2D(graphicsDevice, 1, 1);
		tileTexture.SetData(new[] { Color.White });
		generateMap(seed);
	}

	private void generateMap(int seed) {
		var random = new Random(seed); // Use provided seed for consistent generation

		// Fill with grass first
		for(int x = 0; x < width; x++) {
			for(int y = 0; y < height; y++) {
				tiles[x, y] = new Tile(TileType.Grass);
			}
		}

		// Add some water patches
		for(int i = 0; i < 5; i++) {
			int centerX = random.Next(2, width - 2);
			int centerY = random.Next(2, height - 2);
			int size = random.Next(2, 5);
			for(int x = -size; x <= size; x++) {
				for(int y = -size; y <= size; y++) {
					int tileX = centerX + x;
					int tileY = centerY + y;

					if(tileX >= 0 && tileX < width && tileY >= 0 && tileY < height) {
						if(x * x + y * y <= size * size) // Circular shape
						{
							tiles[tileX, tileY] = new Tile(TileType.Water);
						}
					}
				}
			}
		}

		// Add some trees
		for(int i = 0; i < 20; i++) {
			int x = random.Next(0, width);
			int y = random.Next(0, height);

			if(tiles[x, y].Type == TileType.Grass) {
				tiles[x, y] = new Tile(TileType.Tree);
			}
		}

		// Add a stone path border
		for(int x = 0; x < width; x++) {
			tiles[x, 0] = new Tile(TileType.Stone);
			tiles[x, height - 1] = new Tile(TileType.Stone);
		}
		for(int y = 0; y < height; y++) {
			tiles[0, y] = new Tile(TileType.Stone);
			tiles[width - 1, y] = new Tile(TileType.Stone);
		}
	}

	public void draw(SpriteBatch spriteBatch, Rectangle visibleArea) {
		// Only draw visible tiles (optimization)
		int startX = Math.Max(0, visibleArea.X / tileSize);
		int endX = Math.Min(width, (visibleArea.Right / tileSize) + 1);
		int startY = Math.Max(0, visibleArea.Y / tileSize);
		int endY = Math.Min(height, (visibleArea.Bottom / tileSize) + 1);

		for(int x = startX; x < endX; x++) {
			for(int y = startY; y < endY; y++) {
				var tile = tiles[x, y];
				var destRect = new Rectangle(
					x * tileSize,
					y * tileSize,
					tileSize,
					tileSize
				);
				spriteBatch.Draw(tileTexture, destRect, tile.Color);
			}
		}
	}

	// Get tile at world position
	public Tile getTileAtPosition(Vector2 position) {
		int x = (int)(position.X / tileSize);
		int y = (int)(position.Y / tileSize);
		if(x < 0 || x >= width || y < 0 || y >= height)
			return null;
		return tiles[x, y];
	}

	// Get tile at grid coordinates
	public Tile getTile(int x, int y) {
		if(x < 0 || x >= width || y < 0 || y >= height)
			return null;
		return tiles[x, y];
	}

	// Set tile at grid coordinates
	public void setTile(int x, int y, Tile tile) {
		if(x < 0 || x >= width || y < 0 || y >= height)
			return;
		tiles[x, y] = tile;
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
			var tile = getTileAtPosition(point);
			if(tile != null && !tile.IsWalkable) {
				return true;
			}
		}
		return false;
	}
}