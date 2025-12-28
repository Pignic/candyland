using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.World;

[Serializable]
public class PropData {
	public string propId { get; set; }
	public float x { get; set; }
	public float y { get; set; }
}

[Serializable]
public class DoorData {
	public int direction { get; set; } // 0=North, 1=South, 2=East, 3=West
	public string targetRoomId { get; set; }
	public int targetDirection { get; set; }
}

[Serializable]
public class EnemyData {
	public int behavior { get; set; } 
	public float x { get; set; }
	public float y { get; set; }
	public float speed { get; set; }
	public float detectionRange { get; set; }

	// Patrol data (if applicable)
	public float patrolStartX { get; set; }
	public float patrolStartY { get; set; }
	public float patrolEndX { get; set; }
	public float patrolEndY { get; set; }

	public string spriteKey { get; set; } = "enemy_idle";
	public bool isAnimated { get; set; } = false;
	public int frameCount { get; set; } = 4;
	public int frameWidth { get; set; } = 32;
	public int frameHeight { get; set; } = 32;
	public float frameTime { get; set; } = 0.15f;
}

[Serializable]
public class NPCData {
	public string dialogId { get; set; }
	public float x { get; set; }
	public float y { get; set; }
	public string spriteKey { get; set; }
	public int frameCount { get; set; } = 3;
	public int frameWidth { get; set; } = 32;
	public int frameHeight { get; set; } = 32;
}

[Serializable]
public class MapData {
	public int width { get; set; }
	public int height { get; set; }
	public int tileSize { get; set; }
	public TileType[,] tiles { get; set; }
	public List<DoorData> doors { get; set; }
	public List<EnemyData> enemies { get; set; }
	public List<PropData> props { get; set; }
	public float playerSpawnX { get; set; }
	public float playerSpawnY { get; set; }
	public List<NPCData> NPCs { get; set; } = new List<NPCData>();

	public Dictionary<string, string> tilesetPaths { get; set; } = new Dictionary<string, string>
	{
		{ "Grass", "Assets/Terrain/grass_tileset.png" },
		{ "Water", "Assets/Terrain/water_tileset.png" },
		{ "Stone", "Assets/Terrain/stone_tileset.png" },
		{ "Tree", "Assets/Terrain/tree_tileset.png" }
	};

	public MapData() {
		doors = new List<DoorData>();
		enemies = new List<EnemyData>();
		props = new List<PropData>();
	}

	public MapData(int width, int height, int tileSize) {
		this.width = width;
		this.height = height;
		this.tileSize = tileSize;
		tiles = new TileType[width, height];
		doors = new List<DoorData>();
		enemies = new List<EnemyData>();
		props = new List<PropData>();
		playerSpawnX = (width * tileSize) / 2f;
		playerSpawnY = (height * tileSize) / 2f;

		// Initialize with grass
		for(int x = 0; x < width; x++) {
			for(int y = 0; y < height; y++) {
				tiles[x, y] = TileType.Grass;
			}
		}
	}

	public void SaveToFile(string filepath) {
		// Convert 2D array to 1D for JSON serialization
		var flatData = new {
			Width = width,
			Height = height,
			TileSize = tileSize,
			Tiles = flattenTiles(),
			Doors = doors,
			Enemies = enemies,
			Props = props,
			PlayerSpawnX = playerSpawnX,
			PlayerSpawnY = playerSpawnY
		};

		string json = JsonSerializer.Serialize(flatData, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(filepath, json);
	}

	public static MapData loadFromFile(string filepath) {
		if(!File.Exists(filepath))
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
		foreach(var tileElement in tilesArray.EnumerateArray()) {
			int x = index % width;
			int y = index / width;
			mapData.tiles[x, y] = (TileType)tileElement.GetInt32();
			index++;
		}

		// Load doors (if present)
		if(root.TryGetProperty("Doors", out var doorsElement)) {
			foreach(var doorElement in doorsElement.EnumerateArray()) {
				var doorData = new DoorData {
					direction = doorElement.GetProperty("Direction").GetInt32(),
					targetRoomId = doorElement.GetProperty("TargetRoomId").GetString(),
					targetDirection = doorElement.GetProperty("TargetDirection").GetInt32()
				};
				mapData.doors.Add(doorData);
			}
		}

		// Load enemies (if present)
		if(root.TryGetProperty("Enemies", out var enemiesElement)) {
			foreach(var enemyElement in enemiesElement.EnumerateArray()) {
				var enemyData = new EnemyData {
					behavior = enemyElement.GetProperty("Behavior").GetInt32(),
					x = enemyElement.GetProperty("X").GetSingle(),
					y = enemyElement.GetProperty("Y").GetSingle(),
					speed = enemyElement.GetProperty("Speed").GetSingle(),
					detectionRange = enemyElement.GetProperty("DetectionRange").GetSingle(),
					patrolStartX = enemyElement.GetProperty("PatrolStartX").GetSingle(),
					patrolStartY = enemyElement.GetProperty("PatrolStartY").GetSingle(),
					patrolEndX = enemyElement.GetProperty("PatrolEndX").GetSingle(),
					patrolEndY = enemyElement.GetProperty("PatrolEndY").GetSingle()
				};
				mapData.enemies.Add(enemyData);
			}
		}

		// Load player spawn (if present)
		if(root.TryGetProperty("PlayerSpawnX", out var spawnXElement)) {
			mapData.playerSpawnX = spawnXElement.GetSingle();
		}
		if(root.TryGetProperty("PlayerSpawnY", out var spawnYElement)) {
			mapData.playerSpawnY = spawnYElement.GetSingle();
		}

		// Load props (if present)
		if(root.TryGetProperty("Props", out var propsElement)) {
			foreach(var propElement in propsElement.EnumerateArray()) {
				var propData = new PropData {
					propId = propElement.GetProperty("PropId").GetString(),
					x = propElement.GetProperty("X").GetSingle(),
					y = propElement.GetProperty("Y").GetSingle()
				};
				mapData.props.Add(propData);
			}
		}

		// Load NPCs (if present)
		if(root.TryGetProperty("NPCs", out var npcsElement)) {
			foreach(var npcElement in npcsElement.EnumerateArray()) {
				var npcData = new NPCData {
					dialogId = npcElement.GetProperty("dialogId").GetString(),
					x = (float)npcElement.GetProperty("x").GetDouble(),
					y = (float)npcElement.GetProperty("y").GetDouble(),
					spriteKey = npcElement.GetProperty("spriteKey").GetString(),
					frameCount = npcElement.GetProperty("frameCount").GetInt32(),
					frameWidth = npcElement.GetProperty("frameWidth").GetInt32(),
					frameHeight = npcElement.GetProperty("frameHeight").GetInt32()
				};
				mapData.NPCs.Add(npcData);
			}
		}

		return mapData;
	}

	private int[] flattenTiles() {
		int[] flat = new int[this.width * this.height];
		for(int y = 0; y < this.height; y++) {
			for(int x = 0; x < this.width; x++) {
				flat[y * this.width + x] = (int)tiles[x, y];
			}
		}
		return flat;
	}

	public TileMap toTileMap(Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice) {
		var tileMap = new TileMap(this.width, this.height, this.tileSize, graphicsDevice, 0);
		for(int x = 0; x < this.width; x++) {
			for(int y = 0; y < this.height; y++) {
				tileMap.setTile(x, y, tiles[x, y]);
			}
		}
		return tileMap;
	}
}