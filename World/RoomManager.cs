using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Candyland.Entities;

namespace Candyland.World {
	public class RoomManager {

		public Dictionary<string, Room> rooms { get; private set; }

		public Room currentRoom { get; private set; }

		public RoomManager() {
			rooms = new Dictionary<string, Room>();
		}

		public void addRoom(Room room) {
			rooms[room.id] = room;
		}

		public void setCurrentRoom(string roomId) {
			if(rooms.ContainsKey(roomId)) {
				currentRoom = rooms[roomId];
			}
		}

		public Vector2 getSpawnPositionForDoor(DoorDirection entryDirection) {
			if(currentRoom == null){
				return Vector2.Zero;
			}

			int offset = 64; // Spawn this many pixels away from the door

			switch(entryDirection) {
				case DoorDirection.North:
					return new Vector2(currentRoom.map.pixelWidth / 2, 32);
				case DoorDirection.South:
					return new Vector2(currentRoom.map.pixelWidth / 2, currentRoom.map.pixelHeight - offset);
				case DoorDirection.East:
					return new Vector2(currentRoom.map.pixelWidth - offset, currentRoom.map.pixelHeight / 2);
				case DoorDirection.West:
					return new Vector2(offset, currentRoom.map.pixelHeight / 2);
				default:
					return currentRoom.playerSpawnPosition;
			}
		}

		public void transitionToRoom(string roomId, Player player, DoorDirection entryDirection) {
			setCurrentRoom(roomId);
			if(currentRoom != null) {
				player.Position = getSpawnPositionForDoor(entryDirection);
			}
		}
	}
}