using EldmeresTale.Entities;
using EldmeresTale.Quests;
using EldmeresTale.World;
using EldmoresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Core;

public class RoomLoader {
	private GraphicsDevice _graphicsDevice;
	private AssetManager _assetManager;
	private QuestManager _questManager;

	public RoomLoader(GraphicsDevice graphicsDevice, AssetManager assetManager, QuestManager questManager) {
		_graphicsDevice = graphicsDevice;
		_assetManager = assetManager;
		_questManager = questManager;
	}

	public Room LoadRoom(string roomId, string mapFilePath) {
		// Load map data
		MapData mapData = MapData.loadFromFile(mapFilePath);

		if (mapData == null) {
			System.Diagnostics.Debug.WriteLine($"Failed to load room: {roomId} from {mapFilePath}");
			return null;
		}

		// Create room from map data
		Room room = Room.FromMapData(roomId, mapData, _graphicsDevice);
		if (room != null) {
			room.Map.LoadVariationShader(_assetManager.LoadShader("VariationMask"));
		}

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

		foreach (EnemyData enemyData in mapData.Enemies) {
			// Load enemy sprite
			Texture2D sprite = _assetManager.LoadTexture(GetSpritePathForKey(enemyData.SpriteKey));

			bool isAnimated = sprite.Width == 128 && sprite.Height == 128;

			// Create enemy
			Enemy enemy;

			if (isAnimated) {
				// Use animation data from enemyData, or defaults
				int frameCount = enemyData.FrameCount > 0 ? enemyData.FrameCount : 4;
				int frameWidth = enemyData.FrameWidth > 0 ? enemyData.FrameWidth : 32;
				int frameHeight = enemyData.FrameHeight > 0 ? enemyData.FrameHeight : 32;
				float frameTime = enemyData.FrameTime > 0 ? enemyData.FrameTime : 0.15f;

				enemy = new Enemy(
					sprite,
					new Vector2(enemyData.X, enemyData.Y),
					(EnemyBehavior)enemyData.Behavior,
					frameCount,
					frameWidth,
					frameHeight,
					frameTime,
					speed: enemyData.Speed
				);
			} else {
				// Static sprite
				enemy = new Enemy(
					sprite,
					new Vector2(enemyData.X, enemyData.Y),
					(EnemyBehavior)enemyData.Behavior,
					speed: enemyData.Speed
				);
			}

			enemy.DetectionRange = enemyData.DetectionRange;
			enemy.SetPatrolPoints(
				new Vector2(enemyData.PatrolStartX, enemyData.PatrolStartY),
				new Vector2(enemyData.PatrolEndX, enemyData.PatrolEndY)
			);
			enemy.SetMap(room.Map);
			room.Enemies.Add(enemy);
		}
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

	private string GetSpritePathForKey(string key) {
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
