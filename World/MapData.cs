using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;

namespace Candyland.World
{
    [Serializable]
    public class DoorData
    {
        public int Direction { get; set; } // 0=North, 1=South, 2=East, 3=West
        public string TargetRoomId { get; set; }
        public int TargetDirection { get; set; }
    }

    [Serializable]
	public class EnemyData {
		public int Behavior { get; set; }  // EnemyBehavior enum as int
		public float X { get; set; }
		public float Y { get; set; }
		public float Speed { get; set; }
		public float DetectionRange { get; set; }

		// Patrol data (if applicable)
		public float PatrolStartX { get; set; }
		public float PatrolStartY { get; set; }
		public float PatrolEndX { get; set; }
		public float PatrolEndY { get; set; }

		// NEW: Sprite information
		public string SpriteKey { get; set; } = "enemy_idle";  // Key to look up in asset manager
		public bool IsAnimated { get; set; } = false;
		public int FrameCount { get; set; } = 4;
		public int FrameWidth { get; set; } = 32;
		public int FrameHeight { get; set; } = 32;
		public float FrameTime { get; set; } = 0.15f;
	}

	[Serializable]
	public class NPCData {
		public string DialogId { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public string SpriteKey { get; set; }
		public int FrameCount { get; set; } = 3;
		public int FrameWidth { get; set; } = 32;
		public int FrameHeight { get; set; } = 32;
	}

	[Serializable]
    public class MapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileSize { get; set; }
        public TileType[,] Tiles { get; set; }
        public List<DoorData> Doors { get; set; }
        public List<EnemyData> Enemies { get; set; }
        public float PlayerSpawnX { get; set; }
        public float PlayerSpawnY { get; set; }

		// NPCs (optional - for future)
		public List<NPCData> NPCs { get; set; } = new List<NPCData>();

		// NEW: Tileset configuration
		public Dictionary<string, string> TilesetPaths { get; set; } = new Dictionary<string, string>
		{
		{ "Grass", "Assets/Terrain/grass_tileset.png" },
		{ "Water", "Assets/Terrain/water_tileset.png" },
		{ "Stone", "Assets/Terrain/stone_tileset.png" },
		{ "Tree", "Assets/Terrain/tree_tileset.png" }
	};

		public MapData()
        {
            // Parameterless constructor for serialization
            Doors = new List<DoorData>();
            Enemies = new List<EnemyData>();
        }

        public MapData(int width, int height, int tileSize)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            Tiles = new TileType[width, height];
            Doors = new List<DoorData>();
            Enemies = new List<EnemyData>();
            PlayerSpawnX = (width * tileSize) / 2f;
            PlayerSpawnY = (height * tileSize) / 2f;

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
                Tiles = FlattenTiles(),
                Doors = Doors,
                Enemies = Enemies,
                PlayerSpawnX = PlayerSpawnX,
                PlayerSpawnY = PlayerSpawnY
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

            // Load tiles
            var tilesArray = root.GetProperty("Tiles");
            int index = 0;
            foreach (var tileElement in tilesArray.EnumerateArray())
            {
                int x = index % width;
                int y = index / width;
                mapData.Tiles[x, y] = (TileType)tileElement.GetInt32();
                index++;
            }

            // Load doors (if present)
            if (root.TryGetProperty("Doors", out var doorsElement))
            {
                foreach (var doorElement in doorsElement.EnumerateArray())
                {
                    var doorData = new DoorData
                    {
                        Direction = doorElement.GetProperty("Direction").GetInt32(),
                        TargetRoomId = doorElement.GetProperty("TargetRoomId").GetString(),
                        TargetDirection = doorElement.GetProperty("TargetDirection").GetInt32()
                    };
                    mapData.Doors.Add(doorData);
                }
            }

            // Load enemies (if present)
            if (root.TryGetProperty("Enemies", out var enemiesElement))
            {
                foreach (var enemyElement in enemiesElement.EnumerateArray())
                {
                    var enemyData = new EnemyData
                    {
                        Behavior = enemyElement.GetProperty("Behavior").GetInt32(),
                        X = enemyElement.GetProperty("X").GetSingle(),
                        Y = enemyElement.GetProperty("Y").GetSingle(),
                        Speed = enemyElement.GetProperty("Speed").GetSingle(),
                        DetectionRange = enemyElement.GetProperty("DetectionRange").GetSingle(),
                        PatrolStartX = enemyElement.GetProperty("PatrolStartX").GetSingle(),
                        PatrolStartY = enemyElement.GetProperty("PatrolStartY").GetSingle(),
                        PatrolEndX = enemyElement.GetProperty("PatrolEndX").GetSingle(),
                        PatrolEndY = enemyElement.GetProperty("PatrolEndY").GetSingle()
                    };
                    mapData.Enemies.Add(enemyData);
                }
            }

            // Load player spawn (if present)
            if (root.TryGetProperty("PlayerSpawnX", out var spawnXElement))
            {
                mapData.PlayerSpawnX = spawnXElement.GetSingle();
            }
            if (root.TryGetProperty("PlayerSpawnY", out var spawnYElement))
            {
                mapData.PlayerSpawnY = spawnYElement.GetSingle();
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
                    tileMap.SetTile(x, y, Tiles[x, y]);
                }
            }

            return tileMap;
        }
    }
}