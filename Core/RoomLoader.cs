using EldmeresTale.Entities;
using EldmeresTale.Entities.Definitions;
using EldmeresTale.Entities.Factories;
using EldmeresTale.Quests;
using EldmeresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Core;

public class RoomLoader {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly AssetManager _assetManager;
	private readonly QuestManager _questManager;

	public RoomLoader(GraphicsDevice graphicsDevice, AssetManager assetManager, QuestManager questManager) {
		_graphicsDevice = graphicsDevice;
		_assetManager = assetManager;
		_questManager = questManager;
	}

	public Room LoadRoom(string roomId, string mapFilePath) {
		// Load map data
		MapData mapData = MapData.LoadFromFile(mapFilePath);

		if (mapData == null) {
			System.Diagnostics.Debug.WriteLine($"Failed to load room: {roomId} from {mapFilePath}");
			return null;
		}

		// Create room from map data
		Room room = Room.FromMapData(roomId, mapData, _graphicsDevice);
		room?.Map.LoadVariationShader(_assetManager.LoadShader("VariationMask"));

		// Load tilesets
		LoadTilesetsForRoom(room);

		// Load enemies
		LoadEnemiesForRoom(room, mapData);

		// Load NPCs (if any)
		LoadNPCsForRoom(room, mapData);


		LoadPropsForRoom(room, mapData);

		return room;
	}

	private void LoadTilesetsForRoom(Room room) {
		// Try to load custom tilesets from map data
		foreach (TileDefinition tileDef in TileRegistry.Instance.GetAllTiles()) {
			Texture2D texture = _assetManager.LoadTexture("Assets/Terrain/" + tileDef.TextureName + ".png");
			if (texture != null) {
				room.Map.LoadTileset(tileDef.Id, texture);
			} else {
				System.Diagnostics.Debug.WriteLine($"[Room Loader]Error loading tile with Id {tileDef.Id} and path {tileDef.TextureName}");
			}
		}
	}

	private void LoadEnemiesForRoom(Room room, MapData mapData) {
		if (mapData.Enemies == null || mapData.Enemies.Count == 0) {
			return;
		}

		foreach (EnemySpawnData spawnData in mapData.Enemies) {
			// Load sprite
			EnemyDefinition def = EnemyFactory.Catalog[spawnData.EnemyId];
			Texture2D sprite = _assetManager.LoadTexture($"Assets/Sprites/{def.SpriteKey}.png");

			// Create enemy using factory
			Enemy enemy = EnemyFactory.Create(
				spawnData.EnemyId,
				spawnData,
				sprite,
				room.Map
			);

			if (enemy != null) {
				room.Enemies.Add(enemy);
			}
		}

		System.Diagnostics.Debug.WriteLine($"[ROOM LOADER] Loaded {room.Enemies.Count} enemies");
	}

	private void LoadNPCsForRoom(Room room, MapData mapData) {
		if (mapData.NPCs == null || mapData.NPCs.Count == 0) {
			return;
		}

		foreach (NPCData npcData in mapData.NPCs) {
			string spritePath = GetSpritePathForKey(npcData.SpriteKey);
			Texture2D sprite = _assetManager.LoadTexture(spritePath);

			if (sprite != null) {
				NPC npc = new NPC(
					sprite,
					new Vector2(npcData.X, npcData.Y),
					npcData.DialogId, _questManager,
					npcData.FrameCount,
					npcData.FrameWidth,
					npcData.FrameHeight,
					0.1f
				);
				room.NPCs.Add(npc);
			}
		}
	}

	private void LoadPropsForRoom(Room room, MapData mapData) {
		if (mapData.Props == null || mapData.Props.Count == 0) {
			return;
		}

		foreach (PropData propData in mapData.Props) {
			// Automatically construct sprite path from prop ID
			string spritePath = $"Assets/Sprites/Props/{propData.PropId}.png";
			Texture2D sprite = _assetManager.LoadTexture(spritePath);

			// Create prop using factory
			Prop prop = PropFactory.Create(propData.PropId, sprite, new Vector2(propData.X, propData.Y), _graphicsDevice);

			if (prop != null) {
				room.Props.Add(prop);
			} else {
				System.Diagnostics.Debug.WriteLine($"Failed to create prop: {propData.PropId}");
			}
		}

		System.Diagnostics.Debug.WriteLine($"Loaded {room.Props.Count} props for room {room.Id}");
	}

	private static string GetSpritePathForKey(string key) {
		// Map sprite keys to file paths
		return $"Assets/Sprites/{key}.png";
	}

	public void CreateRooms(RoomManager _roomManager) {
		// Define which rooms to load
		Dictionary<string, string> roomDefinitions = new Dictionary<string, string> {
				{ "room1", "Assets/Maps/room1.json" },
				{ "room2", "Assets/Maps/room2.json" },
				{ "room3", "Assets/Maps/room3.json" }
			};

		// Load all rooms
		foreach ((string roomId, string mapPath) in roomDefinitions) {
			Room room = LoadRoom(roomId, mapPath);
			_roomManager.AddRoom(room);
		}
	}
}
