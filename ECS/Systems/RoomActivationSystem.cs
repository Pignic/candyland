using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components.Tag;

namespace EldmeresTale.ECS.Systems;

public class RoomActivationSystem : AEntitySetSystem<float> {
	private readonly EntitySet _entitiesWithRooms;

	private string _currentRoomId;

	public RoomActivationSystem(World world) : base(world.GetEntities()
			.With<RoomId>()
			.WhenAdded<RoomId>()  // Only triggers when RoomId is added
			.Without<RoomActive>()
			.AsSet()) {
		// All entities with RoomId (for batch updates on room change)
		_entitiesWithRooms = world.GetEntities()
			.With<RoomId>()
			.AsSet();
	}


	protected override void Update(float deltaTime, in Entity entity) {
		// Handle newly spawned entities
		RoomId roomId = entity.Get<RoomId>();
		if (roomId.Name == _currentRoomId) {
			// Entity spawned in current room - activate it
			entity.Set<RoomActive>();
		}
	}

	public void TransitionToRoom(string newRoomId) {
		if (_currentRoomId == newRoomId) {
			return;
		}

		System.Diagnostics.Debug.WriteLine($"[ROOM ACTIVATION] Transitioning: {_currentRoomId} → {newRoomId}");

		_currentRoomId = newRoomId;
		ActivateRoom(newRoomId);
	}

	private void ActivateRoom(string roomId) {
		int activated = 0;
		int deactivated = 0;

		// Batch update all entities with RoomId
		foreach (ref readonly Entity entity in _entitiesWithRooms.GetEntities()) {
			RoomId entityRoom = entity.Get<RoomId>();
			bool isInCurrentRoom = entityRoom.Name == roomId;
			bool hasActiveTag = entity.Has<RoomActive>();

			if (isInCurrentRoom && !hasActiveTag) {
				entity.Set<RoomActive>();
				activated++;
			} else if (!isInCurrentRoom && hasActiveTag) {
				entity.Remove<RoomActive>();
				deactivated++;
			}
		}

		System.Diagnostics.Debug.WriteLine($"[ROOM ACTIVATION] Activated: {activated}, Deactivated: {deactivated}");
	}

	public override void Dispose() {
		_entitiesWithRooms?.Dispose();
		base.Dispose();
	}
}
