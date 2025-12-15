using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Candyland.Entities;
using Candyland.World;
using System;

namespace Candyland.Core {
	/// <summary>
	/// Handles loading rooms from MapData files with automatic asset loading
	/// </summary>
	public class RoomLoader {
		private GraphicsDevice _graphicsDevice;
		private AssetManager _assetManager;
		private Player _player;  // Needed for chase enemies
		private Effect _variationShader;  // ADD THIS

		public RoomLoader(GraphicsDevice graphicsDevice, AssetManager assetManager, Player player, Effect variationShader) {
			_graphicsDevice = graphicsDevice;
			_assetManager = assetManager;
			_player = player;
			this._variationShader = variationShader;
		}

		/// <summary>
		/// Load a room from a JSON file
		/// </summary>
		public Room LoadRoom(string roomId, string mapFilePath) {
			// Load map data
			var mapData = MapData.LoadFromFile(mapFilePath);

			if(mapData == null) {
				System.Diagnostics.Debug.WriteLine($"Failed to load room: {roomId} from {mapFilePath}");
				return null;
			}

			// Create room from map data
			var room = Room.FromMapData(roomId, mapData, _graphicsDevice);

			// Load tilesets
			LoadTilesetsForRoom(room, mapData); 
			
			if(_variationShader != null) {
				room.Map.LoadVariationShader(_variationShader);
			}

			// Load enemies
			LoadEnemiesForRoom(room, mapData);

			// Load NPCs (if any)
			LoadNPCsForRoom(room, mapData);

			return room;
		}

		/// <summary>
		/// Create a fallback procedurally generated room
		/// </summary>
		public Room CreateProceduralRoom(string roomId, int seed, int width = 50, int height = 40, int tileSize = 16) {
			var tileMap = new TileMap(width, height, tileSize, _graphicsDevice, seed);
			var room = new Room(roomId, tileMap, seed);

			// Load default tilesets
			LoadDefaultTilesets(room);
			if(_variationShader != null) {
				room.Map.LoadVariationShader(_variationShader);
			}

			return room;
		}

		private void LoadTilesetsForRoom(Room room, MapData mapData) {
			// Try to load custom tilesets from map data
			if(mapData.TilesetPaths != null && mapData.TilesetPaths.Count > 0) {
				foreach(var kvp in mapData.TilesetPaths) {
					if(Enum.TryParse<TileType>(kvp.Key, out var tileType)) {
						var texture = _assetManager.LoadTexture(kvp.Value);

						if(texture != null) {
							room.Map.LoadTileset(tileType, texture);
						} else {
							// Fallback to generated tileset
							room.Map.LoadTileset(tileType,
								DualGridTilesetGenerator.GenerateTileset(_graphicsDevice, tileType, room.Map.TileSize));
						}
					}
				}
			} else {
				// Use default generated tilesets
				LoadDefaultTilesets(room);
			}
		}

		private void LoadDefaultTilesets(Room room) {
			room.Map.LoadTileset(TileType.Grass,
				DualGridTilesetGenerator.GenerateTileset(_graphicsDevice, TileType.Grass, room.Map.TileSize));
			room.Map.LoadTileset(TileType.Water,
				DualGridTilesetGenerator.GenerateTileset(_graphicsDevice, TileType.Water, room.Map.TileSize));
			room.Map.LoadTileset(TileType.Stone,
				DualGridTilesetGenerator.GenerateTileset(_graphicsDevice, TileType.Stone, room.Map.TileSize));
			room.Map.LoadTileset(TileType.Tree,
				DualGridTilesetGenerator.GenerateTileset(_graphicsDevice, TileType.Tree, room.Map.TileSize));
		}

		private void LoadEnemiesForRoom(Room room, MapData mapData) {
			if(mapData.Enemies == null || mapData.Enemies.Count == 0)
				return;

			foreach(var enemyData in mapData.Enemies) {
				// Load enemy sprite
				string spritePath = GetSpritePathForKey(enemyData.SpriteKey);
				var sprite = _assetManager.LoadTextureOrFallback(spritePath,
					() => CreateFallbackEnemySprite((EnemyBehavior)enemyData.Behavior));

				// Create enemy
				Enemy enemy;

				if(enemyData.IsAnimated) {
					enemy = new Enemy(
						sprite,
						new Vector2(enemyData.X, enemyData.Y),
						(EnemyBehavior)enemyData.Behavior,
						enemyData.FrameCount,
						enemyData.FrameWidth,
						enemyData.FrameHeight,
						enemyData.FrameTime,
						speed: enemyData.Speed
					);
				} else {
					enemy = new Enemy(
						sprite,
						new Vector2(enemyData.X, enemyData.Y),
						(EnemyBehavior)enemyData.Behavior,
						speed: enemyData.Speed
					);
				}

				// Configure behavior-specific settings
				if(enemyData.Behavior == (int)EnemyBehavior.Chase) {
					enemy.DetectionRange = enemyData.DetectionRange;
					enemy.SetChaseTarget(_player, room.Map);
				} else if(enemyData.Behavior == (int)EnemyBehavior.Patrol) {
					enemy.SetPatrolPoints(
						new Vector2(enemyData.PatrolStartX, enemyData.PatrolStartY),
						new Vector2(enemyData.PatrolEndX, enemyData.PatrolEndY)
					);
				}

				room.Enemies.Add(enemy);
			}
		}

		private void LoadNPCsForRoom(Room room, MapData mapData) {
			if(mapData.NPCs == null || mapData.NPCs.Count == 0)
				return;

			foreach(var npcData in mapData.NPCs) {
				string spritePath = GetSpritePathForKey(npcData.SpriteKey);
				var sprite = _assetManager.LoadTexture(spritePath);

				if(sprite != null) {
					var npc = new NPC(
						sprite,
						new Vector2(npcData.X, npcData.Y),
						npcData.DialogId,
						npcData.FrameCount,
						npcData.FrameWidth,
						npcData.FrameHeight,
						0.1f
					);
					room.NPCs.Add(npc);
				}
			}
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
			var (primary, secondary) = behavior switch {
				EnemyBehavior.Idle => (Color.Red, Color.DarkRed),
				EnemyBehavior.Patrol => (Color.Blue, Color.DarkBlue),
				EnemyBehavior.Wander => (Color.Orange, Color.DarkOrange),
				EnemyBehavior.Chase => (Color.Purple, Color.DarkMagenta),
				_ => (Color.Gray, Color.DarkGray)
			};

			return Graphics.CreateColoredTexture(_graphicsDevice, size, size, primary);
		}
	}
}