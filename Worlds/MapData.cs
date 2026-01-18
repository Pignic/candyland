using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.Worlds;

[Serializable]
public class PropData {
	public string PropId { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
}

[Serializable]
public class DoorData {
	public int Direction { get; set; } // 0=North, 1=South, 2=East, 3=West
	public string TargetRoomId { get; set; }
	public int TargetDirection { get; set; }
}

[Serializable]
public class EnemyData {
	public int Behavior { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Speed { get; set; }
	public float DetectionRange { get; set; }

	// Patrol data (if applicable)
	public float PatrolStartX { get; set; }
	public float PatrolStartY { get; set; }
	public float PatrolEndX { get; set; }
	public float PatrolEndY { get; set; }

	public string SpriteKey { get; set; } = "enemy_idle";
	public bool IsAnimated { get; set; } = false;
	public int FrameCount { get; set; } = 4;
	public int FrameWidth { get; set; } = 32;
	public int FrameHeight { get; set; } = 32;
	public float FrameTime { get; set; } = 0.15f;
}

[Serializable]
public class NPCData {
	public string Id { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
}

[Serializable]
public class MapData {
	public int Width { get; set; }
	public int Height { get; set; }
	public int TileSize { get; set; } = 16;
	public int[,] Tiles { get; set; }
	public List<DoorData> Doors { get; set; }
	public List<EnemySpawnData> Enemies { get; set; }
	public List<PropData> Props { get; set; }
	public float PlayerSpawnX { get; set; }
	public float PlayerSpawnY { get; set; }
	public List<NPCData> NPCs { get; set; } = [];

	public MapData() {
		Doors = [];
		Enemies = [];
		Props = [];
	}

	public MapData(int width, int height, int tileSize) {
		Width = width;
		Height = height;
		TileSize = tileSize;
		Tiles = new int[width, height];
		Doors = [];
		Enemies = [];
		Props = [];
		PlayerSpawnX = width * tileSize / 2f;
		PlayerSpawnY = height * tileSize / 2f;

		// Initialize with grass
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				Tiles[x, y] = 1;
			}
		}
	}

	public void SaveToFile(string filepath) {
		// Convert 2D array to 1D for JSON serialization
		var flatData = new {
			Width,
			Height,
			TileSize,
			Tiles = FlattenTiles(),
			Doors,
			Enemies,
			Props,
			PlayerSpawnX,
			PlayerSpawnY
		};

		string json = JsonSerializer.Serialize(flatData, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(filepath, json);
	}

	public static MapData LoadFromFile(string filepath) {
		if (!File.Exists(filepath)) {
			return null;
		}

		string json = File.ReadAllText(filepath);
		JsonDocument doc = JsonDocument.Parse(json);
		JsonElement root = doc.RootElement;

		int width = root.GetProperty("Width").GetInt32();
		int height = root.GetProperty("Height").GetInt32();
		int tileSize = root.GetProperty("TileSize").GetInt32();

		MapData mapData = new MapData(width, height, tileSize);

		// Load tiles
		JsonElement tilesArray = root.GetProperty("Tiles");
		int index = 0;
		foreach (JsonElement tileElement in tilesArray.EnumerateArray()) {
			int x = index % width;
			int y = index / width;
			mapData.Tiles[x, y] = tileElement.GetInt32();
			index++;
		}

		// Load doors (if present)
		if (root.TryGetProperty("Doors", out JsonElement doorsElement)) {
			foreach (JsonElement doorElement in doorsElement.EnumerateArray()) {
				DoorData doorData = new DoorData {
					Direction = doorElement.GetProperty("Direction").GetInt32(),
					TargetRoomId = doorElement.GetProperty("TargetRoomId").GetString(),
					TargetDirection = doorElement.GetProperty("TargetDirection").GetInt32()
				};
				mapData.Doors.Add(doorData);
			}
		}

		// Load player spawn (if present)
		if (root.TryGetProperty("PlayerSpawnX", out JsonElement spawnXElement)) {
			mapData.PlayerSpawnX = spawnXElement.GetSingle();
		}
		if (root.TryGetProperty("PlayerSpawnY", out JsonElement spawnYElement)) {
			mapData.PlayerSpawnY = spawnYElement.GetSingle();
		}

		// Load props (if present)
		if (root.TryGetProperty("Props", out JsonElement propsElement)) {
			foreach (JsonElement propElement in propsElement.EnumerateArray()) {
				PropData propData = new PropData {
					PropId = propElement.GetProperty("PropId").GetString(),
					X = propElement.GetProperty("X").GetSingle(),
					Y = propElement.GetProperty("Y").GetSingle()
				};
				mapData.Props.Add(propData);
			}
		}

		if (root.TryGetProperty("Enemies", out JsonElement enemiesElement)) {
			string enemiesJson = enemiesElement.GetRawText();
			List<EnemySpawnData> enemies = JsonSerializer.Deserialize<List<EnemySpawnData>>(enemiesJson);
			mapData.Enemies = enemies ?? [];
		}

		// Load NPCs (if present)
		if (root.TryGetProperty("NPCs", out JsonElement npcsElement)) {
			foreach (JsonElement npcElement in npcsElement.EnumerateArray()) {
				NPCData npcData = new NPCData {
					Id = npcElement.GetProperty("id").GetString(),
					X = (float)npcElement.GetProperty("x").GetDouble(),
					Y = (float)npcElement.GetProperty("y").GetDouble(),
				};
				mapData.NPCs.Add(npcData);
			}
		}

		return mapData;
	}

	private int[] FlattenTiles() {
		int[] flat = new int[Width * Height];
		for (int y = 0; y < Height; y++) {
			for (int x = 0; x < Width; x++) {
				flat[(y * Width) + x] = Tiles[x, y];
			}
		}
		return flat;
	}

	public TileMap ToTileMap(Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice) {
		TileMap tileMap = new TileMap(Width, Height, TileSize, graphicsDevice);
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				tileMap.SetTile(x, y, Tiles[x, y]);
			}
		}
		return tileMap;
	}
}