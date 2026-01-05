using EldmeresTale.Core;
using EldmeresTale.Entities;
using EldmeresTale.Quests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.World;

public class RoomManager {

	private RoomLoader _roomLoader;

	public Dictionary<string, Room> Rooms { get; private set; }

	public Room CurrentRoom { get; private set; }

	public RoomManager(GraphicsDevice graphicsDevice, AssetManager assetManager, QuestManager questManager) {
		Rooms = new Dictionary<string, Room>();
		_roomLoader = new RoomLoader(
			graphicsDevice,
			assetManager,
			questManager
		);
	}

	public void Load() {
		_roomLoader.CreateRooms(this);
	}

	public void AddRoom(Room room) {
		Rooms[room.Id] = room;
	}

	public void SetCurrentRoom(string roomId) {
		if (Rooms.ContainsKey(roomId)) {
			CurrentRoom = Rooms[roomId];
		}
	}

	public Vector2 GetSpawnPositionForDoor(DoorDirection entryDirection) {
		if (CurrentRoom == null) {
			return Vector2.Zero;
		}

		int offset = 64; // Spawn this many pixels away from the door

		switch (entryDirection) {
			case DoorDirection.North:
				return new Vector2(CurrentRoom.Map.PixelWidth / 2, 32);
			case DoorDirection.South:
				return new Vector2(CurrentRoom.Map.PixelWidth / 2, CurrentRoom.Map.PixelHeight - offset);
			case DoorDirection.East:
				return new Vector2(CurrentRoom.Map.PixelWidth - offset, CurrentRoom.Map.PixelHeight / 2);
			case DoorDirection.West:
				return new Vector2(offset, CurrentRoom.Map.PixelHeight / 2);
			default:
				return CurrentRoom.PlayerSpawnPosition;
		}
	}

	public void transitionToRoom(string roomId, Player player, DoorDirection entryDirection) {
		SetCurrentRoom(roomId);
		if (CurrentRoom != null) {
			player.Position = GetSpawnPositionForDoor(entryDirection);
		}
	}
}