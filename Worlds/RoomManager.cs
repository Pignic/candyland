using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.Entities;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class RoomManager {
	private const int MAX_CACHED_ROOMS = 3;

	private readonly Dictionary<string, Room> _allRooms = []; // Room definitions (maps/doors)
	private readonly LinkedList<string> _visitedRooms = new(); // LRU cache
	private readonly HashSet<string> _loadedRooms = []; // Currently loaded
	private readonly HashSet<string> _preloadedRooms = []; // Adjacent, ready

	private World _world;
	private readonly RoomLoader _roomLoader;

	private EntitySet _allEntitiesWithRooms;

	private string _currentRoomId;
	public RoomManager(GraphicsDevice graphicsDevice, AssetManager assetManager, GameServices gameServices) {
		_roomLoader = new RoomLoader(
			graphicsDevice,
			assetManager,
			gameServices
		);
	}

	public void SetWorld(World world) {
		_world = world;
		_allEntitiesWithRooms = _world.GetEntities().With<RoomId>().AsSet();
	}

	public Room CurrentRoom {
		get { return _currentRoomId != null ? _allRooms[_currentRoomId] : null; }
		private set { }
	}

	// Load room definition (map data, doors)
	public void DefineRoom(string roomId, string mapPath) {
		_allRooms[roomId] = _roomLoader.LoadRoom(roomId, mapPath);
	}

	// Load room entities into World
	public void LoadRoomEntities(string roomId) {
		if (_loadedRooms.Contains(roomId)) {
			return;
		}

		Room room = _allRooms[roomId];
		_roomLoader.SpawnEntities(room); // Creates entities with RoomId
		_loadedRooms.Add(roomId);

		System.Diagnostics.Debug.WriteLine($"[ROOM MANAGER] Loaded entities for {roomId}");
	}

	// Unload room entities from World
	public void UnloadRoomEntities(string roomId) {
		if (!_loadedRooms.Contains(roomId)) {
			return;
		}

		// Dispose all entities with this RoomId
		foreach (Entity e in _allEntitiesWithRooms.GetEntities()) {
			if (e.Get<RoomId>().Name == roomId) {
				e.Dispose();
			}
		}

		_loadedRooms.Remove(roomId);
		System.Diagnostics.Debug.WriteLine($"[ROOM MANAGER] Unloaded entities for {roomId}");
	}

	public void TransitionToRoom(string roomId, Player player, DoorDirection entryDirection) {
		TransitionToRoom(roomId, player);
		ref Position position = ref player.Entity.Get<Position>();
		position.Value = GetSpawnPositionForDoor(roomId, entryDirection);
	}

	// Set current room, manage cache
	public void TransitionToRoom(string roomId, Player player) {
		// Update visit order (LRU)
		_visitedRooms.Remove(roomId); // Remove if exists
		_visitedRooms.AddFirst(roomId); // Add to front

		// If not initialized at all
		if (!_allRooms.ContainsKey(roomId)) {
			DefineRoom(roomId, $"Assets/Maps/{roomId}.json");
		}

		// Load new room if not loaded
		if (!_loadedRooms.Contains(roomId)) {
			LoadRoomEntities(roomId);
		}

		// Set as current
		_currentRoomId = roomId;

		// Update player position
		player.Position = CurrentRoom.PlayerSpawnPosition;

		player.Entity.Set(new RoomId(roomId));

		// Evict oldest rooms if cache full
		while (_visitedRooms.Count > MAX_CACHED_ROOMS) {
			string oldestRoom = _visitedRooms.Last.Value;
			_visitedRooms.RemoveLast();
			UnloadRoomEntities(oldestRoom);
		}

		// Pre-load adjacent rooms (async in future)
		PreloadAdjacentRooms(roomId);
	}

	private void PreloadAdjacentRooms(string roomId) {
		Room room = _allRooms[roomId];
		foreach (Door door in room.Doors) {
			if (!_allRooms.ContainsKey(door.TargetRoomId)) {
				DefineRoom(door.TargetRoomId, $"Assets/Maps/{door.TargetRoomId}.json");
			}
			if (!_loadedRooms.Contains(door.TargetRoomId)) {
				LoadRoomEntities(door.TargetRoomId);
				_preloadedRooms.Add(door.TargetRoomId);
			}
		}
	}

	public HashSet<string> GetAdjacentRooms(string roomId) {
		HashSet<string> adjacent = [];
		if (_allRooms.TryGetValue(roomId, out Room room)) {
			foreach (Door door in room.Doors) {
				adjacent.Add(door.TargetRoomId);
			}
		}
		return adjacent;
	}

	public Vector2 GetSpawnPositionForDoor(string roomId, DoorDirection entryDirection) {
		if (CurrentRoom == null || !_allRooms.TryGetValue(roomId, out Room room)) {
			return Vector2.Zero;
		}
		const int offset = 64; // Spawn this many pixels away from the door
		return entryDirection switch {
			DoorDirection.North => new Vector2(room.Map.PixelWidth / 2, 32),
			DoorDirection.South => new Vector2(room.Map.PixelWidth / 2, room.Map.PixelHeight - offset),
			DoorDirection.East => new Vector2(room.Map.PixelWidth - offset, room.Map.PixelHeight / 2),
			DoorDirection.West => new Vector2(offset, room.Map.PixelHeight / 2),
			_ => room.PlayerSpawnPosition,
		};
	}
}