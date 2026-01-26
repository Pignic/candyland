using EldmeresTale.ECS.Systems;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Systems;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace EldmeresTale.Core.Coordination;

public class RoomTransitionManager {
	private readonly RoomActivationSystem _roomActivationSystem;
	private readonly RoomManager _roomManager;
	private readonly GameEventBus _eventBus;
	private readonly Camera _camera;
	private readonly List<GameSystem> _roomAwareSystems = [];

	public RoomTransitionManager(
		RoomManager roomManager,
		GameEventBus eventBus,
		Camera camera,
		RoomActivationSystem roomActivationSystem
	) {
		_roomManager = roomManager;
		_eventBus = eventBus;
		_camera = camera;
		_roomActivationSystem = roomActivationSystem;
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


	public void SetRoom(Player player, string targetRoomId) {
		string previousRoomId = _roomManager.CurrentRoom?.Id;

		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Set room to {targetRoomId}");

		// Perform room transition
		_roomManager.TransitionToRoom(targetRoomId, player);

		_roomActivationSystem.TransitionToRoom(targetRoomId);

		Room newRoom = _roomManager.CurrentRoom;

		// Notify all room-aware systems
		NotifySystemsOfRoomChange(newRoom);

		// Update camera bounds
		UpdateCameraBounds(newRoom);

		// Publish room changed event
		PublishRoomChangedEvent(previousRoomId, targetRoomId, newRoom);

		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Now in room: {newRoom.Id}, Player pos: {player.Position}");
	}

	public void TransitionToRoom(Player player, string targetRoomId, string targetDoorId) {
		string previousRoomId = _roomManager.CurrentRoom?.Id;
		System.Diagnostics.Debug.WriteLine($"[ROOM TRANSITION] Transitioning from {previousRoomId} to {targetRoomId}");

		// Perform room transition
		_roomManager.TransitionToRoom(targetRoomId, player, targetDoorId);

		_roomActivationSystem.TransitionToRoom(targetRoomId);

		Room newRoom = _roomManager.CurrentRoom;

		// Notify all room-aware systems
		NotifySystemsOfRoomChange(newRoom);

		// Update camera bounds
		UpdateCameraBounds(newRoom);

		// Publish room changed event
		PublishRoomChangedEvent(previousRoomId, targetRoomId, newRoom);

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
		Room newRoom
	) {
		_eventBus.Publish(new RoomChangedEvent {
			PreviousRoomId = previousRoomId,
			NewRoomId = newRoomId,
			NewRoom = newRoom,
		});
	}

	public int RegisteredSystemCount => _roomAwareSystems.Count;
}