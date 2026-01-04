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
	private Player _player;

	public RoomLoader(GraphicsDevice graphicsDevice, AssetManager assetManager, QuestManager questManager, Player player) {
		_graphicsDevice = graphicsDevice;
		_assetManager = assetManager;
		_questManager = questManager;
		_player = player;
	}

	public Room LoadRoom(string roomId, string mapFilePath) {
		// Load map data
		MapData mapData = MapData.loadFromFile(mapFilePath);

		if (mapData == null) {
			System.Diagnostics.Debug.WriteLine($"Failed to load room: {roomId} from {mapFilePath}");
			return null;
		}

		// Create room from map data
		Room room = Room.fromMapData(roomId, mapData, _graphicsDevice);
		if (room != null) {
			room.map.LoadVariationShader(_assetManager.LoadShader("VariationMask"));
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

	public Room CreateProceduralRoom(string roomId, int seed, int width = 50, int height = 40, int tileSize = 16) {
		TileMap tileMap = new TileMap(width, height, tileSize, _graphicsDevice, seed);
		Room room = new Room(roomId, tileMap, seed);
		tileMap.LoadVariationShader(_assetManager.LoadShader("VariationMask"));
		// Load default tilesets
		return room;
	}

	private void LoadTilesetsForRoom(Room room) {
		// Try to load custom tilesets from map data
		foreach (TileDefinition tileDef in TileRegistry.Instance.GetAllTiles()) {
			Texture2D texture = _assetManager.LoadTexture("Assets/Terrain/" + tileDef.TextureName + ".png");
			if (texture != null) {
				room.map.LoadTileset(tileDef.Id, texture);
			} else {
				System.Diagnostics.Debug.WriteLine($"[Room Loader]Error loading tile with Id {tileDef.Id} and path {tileDef.TextureName}");
			}
		}
	}

	private void LoadEnemiesForRoom(Room room, MapData mapData) {
		if (mapData.enemies == null || mapData.enemies.Count == 0) {
			return;
		}

		foreach (EnemyData enemyData in mapData.enemies) {
			// Load enemy sprite
			string spritePath = GetSpritePathForKey(enemyData.spriteKey);
			Texture2D sprite = _assetManager.LoadTextureOrFallback(spritePath,
				() => CreateFallbackEnemySprite((EnemyBehavior)enemyData.behavior));

			// AUTO-DETECT if sprite is animated based on texture dimensions
			// Enemy spritesheets are 128x128 (4 frames x 4 directions = 32x32 each)
			bool isAnimated = sprite.Width == 128 && sprite.Height == 128;

			// Create enemy
			Enemy enemy;

			if (isAnimated) {
				// Use animation data from enemyData, or defaults
				int frameCount = enemyData.frameCount > 0 ? enemyData.frameCount : 4;
				int frameWidth = enemyData.frameWidth > 0 ? enemyData.frameWidth : 32;
				int frameHeight = enemyData.frameHeight > 0 ? enemyData.frameHeight : 32;
				float frameTime = enemyData.frameTime > 0 ? enemyData.frameTime : 0.15f;

				enemy = new Enemy(
					sprite,
					new Vector2(enemyData.x, enemyData.y),
					(EnemyBehavior)enemyData.behavior,
					frameCount,
					frameWidth,
					frameHeight,
					frameTime,
					speed: enemyData.speed
				);
			} else {
				// Static sprite
				enemy = new Enemy(
					sprite,
					new Vector2(enemyData.x, enemyData.y),
					(EnemyBehavior)enemyData.behavior,
					speed: enemyData.speed
				);
			}

			// Configure behavior-specific settings
			if (enemyData.behavior == (int)EnemyBehavior.Chase) {
				enemy.DetectionRange = enemyData.detectionRange;
				enemy.SetChaseTarget(_player);
			} else if (enemyData.behavior == (int)EnemyBehavior.Patrol) {
				enemy.SetPatrolPoints(
					new Vector2(enemyData.patrolStartX, enemyData.patrolStartY),
					new Vector2(enemyData.patrolEndX, enemyData.patrolEndY)
				);
			}
			enemy.SetMap(room.map);
			room.enemies.Add(enemy);
		}
	}

	private void LoadNPCsForRoom(Room room, MapData mapData) {
		if (mapData.NPCs == null || mapData.NPCs.Count == 0) {
			return;
		}

		foreach (NPCData npcData in mapData.NPCs) {
			string spritePath = GetSpritePathForKey(npcData.spriteKey);
			Texture2D sprite = _assetManager.LoadTexture(spritePath);

			if (sprite != null) {
				NPC npc = new NPC(
					sprite,
					new Vector2(npcData.x, npcData.y),
					npcData.dialogId, _questManager,
					npcData.frameCount,
					npcData.frameWidth,
					npcData.frameHeight,
					0.1f
				);
				room.NPCs.Add(npc);
			}
		}
	}

	private void LoadPropsForRoom(Room room, MapData mapData) {
		if (mapData.props == null || mapData.props.Count == 0) {
			return;
		}

		foreach (PropData propData in mapData.props) {
			// Automatically construct sprite path from prop ID
			string spritePath = $"Assets/Sprites/Props/{propData.propId}.png";
			Texture2D sprite = _assetManager.LoadTexture(spritePath);

			// Create prop using factory
			Prop prop = PropFactory.Create(propData.propId, sprite, new Vector2(propData.x, propData.y), _graphicsDevice);

			if (prop != null) {
				room.props.Add(prop);
			} else {
				System.Diagnostics.Debug.WriteLine($"Failed to create prop: {propData.propId}");
			}
		}

		System.Diagnostics.Debug.WriteLine($"Loaded {room.props.Count} props for room {room.id}");
	}

	private string GetSpritePathForKey(string key) {
		// Map sprite keys to file paths
		return key switch {
			"enemy_idle" => "Assets/Sprites/enemy_idle.png",
			"enemy_patrol" => "Assets/Sprites/enemy_patrol.png",
			"enemy_wander" => "Assets/Sprites/enemy_wander.png",
			"enemy_chase" => "Assets/Sprites/enemy_chase.png",
			"player" => "Assets/Sprites/player.png",
			"quest_giver_forest" => "Assets/Sprites/quest_giver_forest.png",
			_ => $"Assets/Sprites/{key}.png"
		};
	}

	private Texture2D CreateFallbackEnemySprite(EnemyBehavior behavior) {
		int size = 16;
		(Color primary, Color secondary) = behavior switch {
			EnemyBehavior.Idle => (Color.Red, Color.DarkRed),
			EnemyBehavior.Patrol => (Color.Blue, Color.DarkBlue),
			EnemyBehavior.Wander => (Color.Orange, Color.DarkOrange),
			EnemyBehavior.Chase => (Color.Purple, Color.DarkMagenta),
			_ => (Color.Gray, Color.DarkGray)
		};

		return Graphics.CreateColoredTexture(_graphicsDevice, size, size, primary);
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

			if (room != null) {
				_roomManager.addRoom(room);
			} else {
				// Fallback to procedural generation
				System.Diagnostics.Debug.WriteLine($"Failed to load {roomId}, generating procedural room");
				Room proceduralRoom = CreateProceduralRoom(roomId, roomId.GetHashCode());
				_roomManager.addRoom(proceduralRoom);
			}
		}
	}
}