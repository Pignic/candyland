using EldmeresTale.Core;
using EldmeresTale.Entities;
using EldmeresTale.World;

namespace EldmeresTale.Systems;

public class RoomTransitionSystem : GameSystem {
	private RoomManager _roomManager;
	private Player _player;
	private Camera _camera;

	//public RoomTransition? CheckForTransition() {
	//	var door = _roomManager.currentRoom.checkDoorCollision(_player.Bounds);

	//	if(door != null) {
	//		return new RoomTransition {
	//			TargetRoomId = door.targetRoomId,
	//			TargetDirection = door.targetDoorDirection,
	//			SpawnPosition = _roomManager.getSpawnPositionForDoor(door.targetDoorDirection)
	//		};
	//	}

	//	return null;
	//}

	//public void ExecuteTransition(RoomTransition transition, List<Enemy> enemies, List<Pickup> pickups) {
	//	_roomManager.transitionToRoom(transition.TargetRoomId, _player, transition.TargetDirection);

	//	// Update references to new room's entities
	//	enemies.Clear();
	//	enemies.AddRange(_roomManager.currentRoom.enemies);

	//	pickups.Clear();
	//	pickups.AddRange(_roomManager.currentRoom.pickups);

	//	// Update camera bounds
	//	_camera.WorldBounds = new Rectangle(
	//		0, 0,
	//		_roomManager.currentRoom.map.pixelWidth,
	//		_roomManager.currentRoom.map.pixelHeight
	//	);
	//}
}
