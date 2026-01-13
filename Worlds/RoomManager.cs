using EldmeresTale.Core;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Worlds;

public class RoomManager {

	private readonly RoomLoader _roomLoader;

	public Dictionary<string, Room> Rooms { get; }

	public Room CurrentRoom { get; private set; }

	public RoomManager(GraphicsDevice graphicsDevice, AssetManager assetManager, GameServices gameServices) {
		Rooms = [];
		_roomLoader = new RoomLoader(
			graphicsDevice,
			assetManager,
			gameServices
		);
	}

	public void Load() {
		_roomLoader.CreateRooms(this);
	}

	public void AddRoom(Room room) {
		Rooms[room.Id] = room;
	}

	public void SetCurrentRoom(string roomId) {
		if (Rooms.TryGetValue(roomId, out Room value)) {
			CurrentRoom = value;
		}
	}

	public Vector2 GetSpawnPositionForDoor(DoorDirection entryDirection) {
		if (CurrentRoom == null) {
			return Vector2.Zero;
		}

		const int offset = 64; // Spawn this many pixels away from the door

		return entryDirection switch {
			DoorDirection.North => new Vector2(CurrentRoom.Map.PixelWidth / 2, 32),
			DoorDirection.South => new Vector2(CurrentRoom.Map.PixelWidth / 2, CurrentRoom.Map.PixelHeight - offset),
			DoorDirection.East => new Vector2(CurrentRoom.Map.PixelWidth - offset, CurrentRoom.Map.PixelHeight / 2),
			DoorDirection.West => new Vector2(offset, CurrentRoom.Map.PixelHeight / 2),
			_ => CurrentRoom.PlayerSpawnPosition,
		};
	}

	public void TransitionToRoom(string roomId, Player player, DoorDirection entryDirection) {
		SetCurrentRoom(roomId);
		if (CurrentRoom != null) {
			player.Position = GetSpawnPositionForDoor(entryDirection);
		}
	}
}