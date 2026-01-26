using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.Entities;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using System;
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
	private EntitySet _allDoors;

	private string _currentRoomId;

	public RoomManager(AssetManager assetManager, GameServices gameServices) {
		_roomLoader = new RoomLoader(
			assetManager,
			gameServices
		);
	}

	public void SetWorld(World world) {
		_world = world;
		_allEntitiesWithRooms = _world.GetEntities().With<RoomId>().AsSet();
		_allDoors = _world.GetEntities().With<RoomTransition>().AsSet();
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

	public void TransitionToRoom(string roomId, Player player, string targetDoorId) {
		TransitionToRoom(roomId, player);
		ref Position position = ref player.Entity.Get<Position>();
		Collider collider = player.Entity.Get<Collider>();
		position.Value = GetSpawnPositionForDoor(roomId, targetDoorId, new Vector2(collider.Width, collider.Height));
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
		if (_allRooms.TryGetValue(roomId, out Room room)) {
			foreach (Entity door in room.Doors.Values) {
				RoomTransition RoomTransition = door.Get<RoomTransition>();
				string targetRoomId = door.Get<RoomTransition>().TargetRoomId;
				if (!_allRooms.ContainsKey(door.Get<RoomTransition>().TargetRoomId)) {
					DefineRoom(targetRoomId, $"Assets/Maps/{targetRoomId}.json");
				}
				if (!_loadedRooms.Contains(targetRoomId)) {
					LoadRoomEntities(targetRoomId);
					_preloadedRooms.Add(targetRoomId);
				}
			}
		}
	}

	public HashSet<string> GetAdjacentRooms(string roomId) {
		HashSet<string> adjacent = [];
		if (_allRooms.TryGetValue(roomId, out Room room)) {
			foreach (Entity door in room.Doors.Values) {
				adjacent.Add(door.Get<RoomTransition>().TargetRoomId);
			}
		}
		return adjacent;
	}

	public Vector2 GetSpawnPositionForDoor(string roomId, string doorId, Vector2 spawnedEntitySize) {
		if (CurrentRoom == null || !_allRooms.TryGetValue(roomId, out Room room) || doorId == null) {
			return Vector2.Zero;
		}
		Entity doorEntity = room.Doors[doorId];
		RoomTransition roomTransition = doorEntity.Get<RoomTransition>();
		Rectangle doorShape = doorEntity.Get<Collider>().GetBounds(doorEntity.Get<Position>());
		Vector2 position = new Vector2(doorShape.Location.X, doorShape.Location.Y);
		switch (roomTransition.Direction) {
			case EldmeresTale.ECS.Direction.Left: position += new Vector2(doorShape.Width + -(spawnedEntitySize.X / 2), doorShape.Height / 2f); break;
			case EldmeresTale.ECS.Direction.Down: position += new Vector2(doorShape.Width / 2f, doorShape.Height); break;
			case EldmeresTale.ECS.Direction.Right: position += new Vector2(spawnedEntitySize.X / 2, doorShape.Height / 2f); break;
			case EldmeresTale.ECS.Direction.Up: position += new Vector2(doorShape.Width / 2f, spawnedEntitySize.Y); break;
		}
		Vector2 offset = Vector2.UnitX * -16f;
		offset.Rotate((int)roomTransition.Direction * MathF.PI / 2f);
		return position + offset;
	}
}