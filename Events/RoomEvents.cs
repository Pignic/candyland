using EldmeresTale.World;

namespace EldmeresTale.Events;

public class RoomChangedEvent : GameEvent {
	public string PreviousRoomId { get; set; }
	public string NewRoomId { get; set; }
	public Room NewRoom { get; set; }
	public DoorDirection EntryDirection { get; set; }
}
