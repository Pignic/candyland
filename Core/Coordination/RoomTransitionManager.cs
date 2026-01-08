using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Systems;
using EldmeresTale.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace EldmeresTale.Core.Coordination;

public class RoomTransitionManager {
	private readonly RoomManager _roomManager;
	private readonly GameEventBus _eventBus;
	private readonly Camera _camera;
	private readonly List<GameSystem> _roomAwareSystems = [];

	public RoomTransitionManager(
		RoomManager roomManager,
		GameEventBus eventBus,
		Camera camera
	) {
		_roomManager = roomManager;
		_eventBus = eventBus;
		_camera = camera;
	}

	public void RegisterSystem(GameSystem system) {
		if (!_roomAwareSystems.Contains(system)) {
			_roomAwareSystems.Add(system);
			System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Registered system: {system.GetType().Name}");
		}
	}

	public void UnregisterSystem(GameSystem system) {
		_roomAwareSystems.Remove(system);
		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Unregistered system: {system.GetType().Name}");
	}

	public void CheckAndTransition(Player player) {
		Door door = _roomManager.CurrentRoom.CheckDoorCollision(player.Bounds);
		if (door == null) {
			return;
		}

		// Perform transition
		TransitionToRoom(door.TargetRoomId, player, door.TargetDoorDirection);
	}

	public void TransitionToRoom(string targetRoomId, Player player, DoorDirection entryDirection) {
		string previousRoomId = _roomManager.CurrentRoom?.Id;

		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Transitioning from {previousRoomId} to {targetRoomId}");

		// Perform room transition
		_roomManager.TransitionToRoom(targetRoomId, player, entryDirection);

		Room newRoom = _roomManager.CurrentRoom;

		// Notify all room-aware systems
		NotifySystemsOfRoomChange(newRoom);

		// Update camera bounds
		UpdateCameraBounds(newRoom);

		// Publish room changed event
		PublishRoomChangedEvent(previousRoomId, targetRoomId, newRoom, entryDirection);

		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Now in room: {newRoom.Id}, Player pos: {player.Position}");
	}

	private void NotifySystemsOfRoomChange(Room newRoom) {
		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Notifying {_roomAwareSystems.Count} systems");

		foreach (GameSystem system in _roomAwareSystems) {
			try {
				system.OnRoomChanged(newRoom);
				System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Updated {system.GetType().Name}");
			} catch (System.Exception ex) {
				System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Error updating {system.GetType().Name}: {ex.Message}");
			}
		}
	}

	private void UpdateCameraBounds(Room room) {
		_camera.WorldBounds = new Rectangle(
			0, 0,
			room.Map.PixelWidth,
			room.Map.PixelHeight
		);

		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Camera bounds updated: {room.Map.PixelWidth}x{room.Map.PixelHeight}");
	}

	private void PublishRoomChangedEvent(
		string previousRoomId,
		string newRoomId,
		Room newRoom,
		DoorDirection entryDirection
	) {
		_eventBus.Publish(new RoomChangedEvent {
			PreviousRoomId = previousRoomId,
			NewRoomId = newRoomId,
			NewRoom = newRoom,
			EntryDirection = entryDirection
		});
	}

	public int RegisteredSystemCount => _roomAwareSystems.Count;
}