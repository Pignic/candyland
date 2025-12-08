using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Candyland.Entities;

namespace Candyland.World
{
    public class RoomManager
    {
        private Dictionary<string, Room> _rooms;
        public Room CurrentRoom { get; private set; }

        public RoomManager()
        {
            _rooms = new Dictionary<string, Room>();
        }

        public void AddRoom(Room room)
        {
            _rooms[room.Id] = room;
        }

        public void SetCurrentRoom(string roomId)
        {
            if (_rooms.ContainsKey(roomId))
            {
                CurrentRoom = _rooms[roomId];
            }
        }

        public Vector2 GetSpawnPositionForDoor(DoorDirection entryDirection)
        {
            if (CurrentRoom == null)
                return Vector2.Zero;

            int offset = 64; // Spawn this many pixels away from the door

            switch (entryDirection)
            {
                case DoorDirection.North:
                    return new Vector2(CurrentRoom.Map.PixelWidth / 2, offset);
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

        public void TransitionToRoom(string roomId, Player player, DoorDirection entryDirection)
        {
            SetCurrentRoom(roomId);

            if (CurrentRoom != null)
            {
                Vector2 spawnPos = GetSpawnPositionForDoor(entryDirection);
                player.Position = spawnPos;
            }
        }
    }
}