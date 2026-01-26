namespace EldmeresTale.ECS.Components;

struct RoomTransition {

	public string DoorId;
	public string TargetDoorID;
	public string TargetRoomId;
	public Direction Direction;

	public RoomTransition(string doorId, Direction direction, string targetDoorId, string targetRoomId) {
		DoorId = doorId;
		Direction = direction;
		TargetDoorID = targetDoorId;
		TargetRoomId = targetRoomId;
	}
}
