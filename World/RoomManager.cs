using Candyland.Core;
using Candyland.Entities;
using Candyland.Quests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Candyland.World {
	public class RoomManager {

		private RoomLoader roomLoader;

		public Dictionary<string, Room> rooms { get; private set; }

		public Room currentRoom { get; private set; }

		private AssetManager assetManager;

		private QuestManager questManager;

		public RoomManager(GraphicsDevice graphicsDevice, AssetManager assetManager, QuestManager questManager, Player player) {
			this.assetManager = assetManager;
			this.questManager = questManager;
			rooms = new Dictionary<string, Room>();

			roomLoader = new RoomLoader(
				graphicsDevice,
				assetManager,
				questManager,
				player
			);

			roomLoader.setPlayer(player);
		}

		public void Load() {
			roomLoader.CreateRooms(this);

			// TODO: load the NPC from the config
			var room1 = rooms["room1"];
			var questGiverSprite = assetManager.LoadTexture("Assets/Sprites/quest_giver_forest.png");
			if(questGiverSprite != null && room1 != null) {
				var questGiver = new NPC(
					questGiverSprite,
					new Vector2(400, 300),
					"shepherd", questManager,
					3, 32, 32, 0.1f,
					width: 24, height: 24
				);
				room1.NPCs.Add(questGiver);
			}
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