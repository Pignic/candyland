using System;
using System.IO;
using System.Text.Json;

namespace Candyland.World
{
    [Serializable]
    public class MapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileSize { get; set; }
        public TileType[,] Tiles { get; set; }

        public MapData()
        {
            // Parameterless constructor for serialization
        }

        public MapData(int width, int height, int tileSize)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            Tiles = new TileType[width, height];

            // Initialize with grass
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tiles[x, y] = TileType.Grass;
                }
            }
        }

        public void SaveToFile(string filepath)
        {
            // Convert 2D array to 1D for JSON serialization
            var flatData = new
            {
                Width = Width,
                Height = Height,
                TileSize = TileSize,
                Tiles = FlattenTiles()
            };

            string json = JsonSerializer.Serialize(flatData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filepath, json);
        }

        public static MapData LoadFromFile(string filepath)
        {
            if (!File.Exists(filepath))
                return null;

            string json = File.ReadAllText(filepath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            int width = root.GetProperty("Width").GetInt32();
            int height = root.GetProperty("Height").GetInt32();
            int tileSize = root.GetProperty("TileSize").GetInt32();

            var mapData = new MapData(width, height, tileSize);

            var tilesArray = root.GetProperty("Tiles");
            int index = 0;
            foreach (var tileElement in tilesArray.EnumerateArray())
            {
                int x = index % width;
                int y = index / width;
                mapData.Tiles[x, y] = (TileType)tileElement.GetInt32();
                index++;
            }

            return mapData;
        }

        private int[] FlattenTiles()
        {
            int[] flat = new int[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    flat[y * Width + x] = (int)Tiles[x, y];
                }
            }
            return flat;
        }

        public TileMap ToTileMap(Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice)
        {
            var tileMap = new TileMap(Width, Height, TileSize, graphicsDevice, 0); // seed doesn't matter

            // Override generated tiles with saved data
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    tileMap.SetTile(x, y, new Tile(Tiles[x, y]));
                }
            }

            return tileMap;
        }
    }
}