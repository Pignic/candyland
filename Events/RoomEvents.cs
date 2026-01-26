using EldmeresTale.Worlds;

namespace EldmeresTale.Events;

public abstract class RoomEvent : GameEvent {
	public string DoorId { get; set; }
	public string TargetDoorId { get; set; }
	public string PreviousRoomId { get; set; }
	public string NewRoomId { get; set; }
	public Room NewRoom { get; set; }
}

public class RoomChangedEvent : RoomEvent {

}

public class RoomChangingEvent : RoomEvent {

}