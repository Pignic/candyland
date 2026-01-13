using DefaultEcs;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Worlds;

public enum DoorDirection {
	North,
	South,
	East,
	West
}

public class Door {
	public DoorDirection Direction { get; set; }
	public Rectangle Bounds { get; set; }
	public string TargetRoomId { get; set; }
	public DoorDirection TargetDoorDirection { get; set; }
	public Color Color { get; set; } = Color.Brown;

	public Door(DoorDirection direction, Rectangle bounds, string targetRoomId, DoorDirection targetDoorDirection) {
		Direction = direction;
		Bounds = bounds;
		TargetRoomId = targetRoomId;
		TargetDoorDirection = targetDoorDirection;
	}
}

public class Room {
	public string Id { get; set; }
	public TileMap Map { get; set; }
	public List<Entity> Enemies { get; set; }
	public List<Pickup> Pickups { get; set; }
	public List<Door> Doors { get; set; }
	public List<NPC> NPCs { get; }
	public List<Entity> Props { get; }

	public Vector2 PlayerSpawnPosition { get; set; }

	public Room(string id, TileMap map) {
		Id = id;
		Map = map;
		Enemies = [];
		Pickups = [];
		Doors = [];
		NPCs = [];
		Props = [];
		PlayerSpawnPosition = new Vector2(map.PixelWidth / 2, map.PixelHeight / 2);
	}

	// Create a room from MapData
	public static Room FromMapData(string roomId, MapData mapData, GraphicsDevice graphicsDevice) {
		TileMap tileMap = mapData.ToTileMap(graphicsDevice);
		Room room = new Room(roomId, tileMap) {
			PlayerSpawnPosition = new Vector2(mapData.PlayerSpawnX, mapData.PlayerSpawnY)
		};

		// Load doors
		foreach (DoorData doorData in mapData.Doors) {
			room.AddDoor(
				(DoorDirection)doorData.Direction,
				doorData.TargetRoomId,
				(DoorDirection)doorData.TargetDirection
			);
		}
		return room;
	}

	public void AddDoor(DoorDirection direction, string targetRoomId, DoorDirection targetDoorDirection) {
		const int doorWidth = 32;
		const int doorHeight = 16;
		Rectangle doorBounds = direction switch {
			DoorDirection.North => new Rectangle(
				(Map.PixelWidth / 2) - (doorWidth / 2),
				0,
				doorWidth,
				doorHeight
			),
			DoorDirection.South => new Rectangle(
				(Map.PixelWidth / 2) - (doorWidth / 2),
				Map.PixelHeight - doorHeight,
				doorWidth,
				doorHeight
			),
			DoorDirection.East => new Rectangle(
				Map.PixelWidth - doorHeight,
				(Map.PixelHeight / 2) - (doorWidth / 2),
				doorHeight,
				doorWidth
			),
			DoorDirection.West => new Rectangle(
				0,
				(Map.PixelHeight / 2) - (doorWidth / 2),
				doorHeight,
				doorWidth
			),
			_ => Rectangle.Empty,
		};
		Doors.Add(new Door(direction, doorBounds, targetRoomId, targetDoorDirection));
	}

	public void DrawDoors(SpriteBatch spriteBatch, Texture2D doorTexture) {
		foreach (Door door in Doors) {
			spriteBatch.Draw(doorTexture, door.Bounds, door.Color);
		}
	}

	public Door CheckDoorCollision(Rectangle entityBounds) {
		foreach (Door door in Doors) {
			if (door.Bounds.Intersects(entityBounds)) {
				return door;
			}
		}
		return null;
	}
}