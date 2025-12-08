using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.World
{
    public class TileMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; }

        private Tile[,] _tiles;
        private Texture2D _tileTexture;

        public int PixelWidth => Width * TileSize;
        public int PixelHeight => Height * TileSize;

        public TileMap(int width, int height, int tileSize, GraphicsDevice graphicsDevice, int seed = 42)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            _tiles = new Tile[width, height];

            // Create a simple 1x1 white texture for drawing colored tiles
            _tileTexture = new Texture2D(graphicsDevice, 1, 1);
            _tileTexture.SetData(new[] { Color.White });

            GenerateMap(seed);
        }

        private void GenerateMap(int seed)
        {
            var random = new Random(seed); // Use provided seed for consistent generation

            // Fill with grass first
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _tiles[x, y] = new Tile(TileType.Grass);
                }
            }

            // Add some water patches
            for (int i = 0; i < 5; i++)
            {
                int centerX = random.Next(2, Width - 2);
                int centerY = random.Next(2, Height - 2);
                int size = random.Next(2, 5);

                for (int x = -size; x <= size; x++)
                {
                    for (int y = -size; y <= size; y++)
                    {
                        int tileX = centerX + x;
                        int tileY = centerY + y;

                        if (tileX >= 0 && tileX < Width && tileY >= 0 && tileY < Height)
                        {
                            if (x * x + y * y <= size * size) // Circular shape
                            {
                                _tiles[tileX, tileY] = new Tile(TileType.Water);
                            }
                        }
                    }
                }
            }

            // Add some trees
            for (int i = 0; i < 20; i++)
            {
                int x = random.Next(0, Width);
                int y = random.Next(0, Height);

                if (_tiles[x, y].Type == TileType.Grass)
                {
                    _tiles[x, y] = new Tile(TileType.Tree);
                }
            }

            // Add a stone path border
            for (int x = 0; x < Width; x++)
            {
                _tiles[x, 0] = new Tile(TileType.Stone);
                _tiles[x, Height - 1] = new Tile(TileType.Stone);
            }
            for (int y = 0; y < Height; y++)
            {
                _tiles[0, y] = new Tile(TileType.Stone);
                _tiles[Width - 1, y] = new Tile(TileType.Stone);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle visibleArea)
        {
            // Only draw visible tiles (optimization)
            int startX = Math.Max(0, visibleArea.X / TileSize);
            int endX = Math.Min(Width, (visibleArea.Right / TileSize) + 1);
            int startY = Math.Max(0, visibleArea.Y / TileSize);
            int endY = Math.Min(Height, (visibleArea.Bottom / TileSize) + 1);

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    var tile = _tiles[x, y];
                    var destRect = new Rectangle(
                        x * TileSize,
                        y * TileSize,
                        TileSize,
                        TileSize
                    );

                    spriteBatch.Draw(_tileTexture, destRect, tile.Color);
                }
            }
        }

        // Get tile at world position
        public Tile GetTileAtPosition(Vector2 position)
        {
            int x = (int)(position.X / TileSize);
            int y = (int)(position.Y / TileSize);

            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;

            return _tiles[x, y];
        }

        // Get tile at grid coordinates
        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;

            return _tiles[x, y];
        }

        // Set tile at grid coordinates
        public void SetTile(int x, int y, Tile tile)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            _tiles[x, y] = tile;
        }

        // Check if a rectangle collides with unwalkable tiles
        public bool CheckCollision(Rectangle bounds)
        {
            // Check all four corners and center of the entity
            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(bounds.Left, bounds.Top),
                new Vector2(bounds.Right - 1, bounds.Top),
                new Vector2(bounds.Left, bounds.Bottom - 1),
                new Vector2(bounds.Right - 1, bounds.Bottom - 1),
                new Vector2(bounds.Center.X, bounds.Center.Y)
            };

            foreach (var point in checkPoints)
            {
                var tile = GetTileAtPosition(point);
                if (tile != null && !tile.IsWalkable)
                {
                    return true;
                }
            }

            return false;
        }
    }
}