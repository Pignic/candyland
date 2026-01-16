using DefaultEcs;
using EldmeresTale.Entities;
using EldmeresTale.Quests;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core;

public class RoomLoader {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly AssetManager _assetManager;
	private readonly QuestManager _questManager;
	private readonly GameServices _gameServices;

	public RoomLoader(GraphicsDevice graphicsDevice, AssetManager assetManager, GameServices gameServices) {
		_graphicsDevice = graphicsDevice;
		_assetManager = assetManager;
		_questManager = gameServices.QuestManager;
		_gameServices = gameServices;
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
		return room;
	}

	public void SpawnEntities(Room room) {
		LoadEnemiesForRoom(room);
		LoadNPCsForRoom(room);
		LoadPropsForRoom(room);
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

	private void LoadEnemiesForRoom(Room room) {
		MapData mapData = room.MapData;
		if (mapData.Enemies == null || mapData.Enemies.Count == 0) {
			return;
		}

		foreach (EnemySpawnData spawnData in mapData.Enemies) {
			Entity enemy = _gameServices.EnemyFactory.Create(room.Id, spawnData);
			room.Enemies.Add(enemy);
		}

		System.Diagnostics.Debug.WriteLine($"[ROOM LOADER] Loaded {room.Enemies.Count} enemies");
	}

	private void LoadNPCsForRoom(Room room) {
		MapData mapData = room.MapData;
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

	private void LoadPropsForRoom(Room room) {
		MapData mapData = room.MapData;
		if (mapData.Props == null || mapData.Props.Count == 0) {
			return;
		}

		foreach (PropData propData in mapData.Props) {
			room.Props.Add(_gameServices.PropFactory.Create(room.Id, propData.PropId, new Vector2(propData.X, propData.Y)));
		}

		System.Diagnostics.Debug.WriteLine($"Loaded {room.Props.Count} props for room {room.Id}");
	}

	private static string GetSpritePathForKey(string key) {
		// Map sprite keys to file paths
		return $"Assets/Sprites/{key}.png";
	}
}
