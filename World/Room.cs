using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using EldmeresTale.Entities;

namespace EldmeresTale.World {

	public enum DoorDirection {
		North,
		South,
		East,
		West
	}

	public class Door {
		public DoorDirection direction { get; set; }
		public Rectangle bounds { get; set; }
		public string targetRoomId { get; set; }
		public DoorDirection targetDoorDirection { get; set; }
		public Color color { get; set; } = Color.Brown;

		public Door(DoorDirection direction, Rectangle bounds, string targetRoomId, DoorDirection targetDoorDirection) {
			this.direction = direction;
			this.bounds = bounds;
			this.targetRoomId = targetRoomId;
			this.targetDoorDirection = targetDoorDirection;
		}
	}

	public class Room {
		public string id { get; set; }
		public TileMap map { get; set; }
		public List<Enemy> enemies { get; set; }
		public List<Pickup> pickups { get; set; }
		public List<Door> doors { get; set; }
		public List<NPC> NPCs { get; private set; }
		public List<Prop> props { get; private set; }

		public Vector2 playerSpawnPosition { get; set; }
		public int seed { get; set; }

		public Room(string id, TileMap map, int seed) {
			this.id = id;
			this.map = map;
			this.seed = seed;
			enemies = new List<Enemy>();
			pickups = new List<Pickup>();
			doors = new List<Door>();
			NPCs = new List<NPC>();
			props = new List<Prop>();
			playerSpawnPosition = new Vector2(map.PixelWidth / 2, map.PixelHeight / 2);
		}

		// Create a room from MapData
		public static Room fromMapData(string roomId, MapData mapData, GraphicsDevice graphicsDevice) {
			var tileMap = mapData.toTileMap(graphicsDevice);
			var room = new Room(roomId, tileMap, 0);

			// Set player spawn
			room.playerSpawnPosition = new Vector2(mapData.playerSpawnX, mapData.playerSpawnY);

			// Load doors
			foreach(var doorData in mapData.doors) {
				room.addDoor(
					(DoorDirection)doorData.direction,
					doorData.targetRoomId,
					(DoorDirection)doorData.targetDirection
				);
			}

			return room;
		}

		public void addDoor(DoorDirection direction, string targetRoomId, DoorDirection targetDoorDirection) {
			Rectangle doorBounds;
			int doorWidth = 64;
			int doorHeight = 32;

			switch(direction) {
				case DoorDirection.North:
					doorBounds = new Rectangle(
						map.PixelWidth / 2 - doorWidth / 2,
						0,
						doorWidth,
						doorHeight
					);
					break;
				case DoorDirection.South:
					doorBounds = new Rectangle(
						map.PixelWidth / 2 - doorWidth / 2,
						map.PixelHeight - doorHeight,
						doorWidth,
						doorHeight
					);
					break;
				case DoorDirection.East:
					doorBounds = new Rectangle(
						map.PixelWidth - doorHeight,
						map.PixelHeight / 2 - doorWidth / 2,
						doorHeight,
						doorWidth
					);
					break;
				case DoorDirection.West:
					doorBounds = new Rectangle(
						0,
						map.PixelHeight / 2 - doorWidth / 2,
						doorHeight,
						doorWidth
					);
					break;
				default:
					doorBounds = Rectangle.Empty;
					break;
			}

			doors.Add(new Door(direction, doorBounds, targetRoomId, targetDoorDirection));
		}

		public void drawDoors(SpriteBatch spriteBatch, Texture2D doorTexture) {
			foreach(var door in doors) {
				spriteBatch.Draw(doorTexture, door.bounds, door.color);
			}
		}

		public Door checkDoorCollision(Rectangle entityBounds) {
			foreach(var door in doors) {
				if(door.bounds.Intersects(entityBounds)) {
					return door;
				}
			}
			return null;
		}
	}
}