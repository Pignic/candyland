using System.Collections.Generic;

namespace Candyland.Core.Saves;

public class WorldSaveData {
	public string CurrentRoomId { get; set; }

	public Dictionary<string, string> GameFlags { get; set; }

	// TODO: add:
	// public List<string> DefeatedBosses { get; set; }
	// public Dictionary<string, RoomState> RoomStates { get; set; }
	// public List<string> DiscoveredLocations { get; set; }

	public WorldSaveData() {
		CurrentRoomId = "room1";  // Default starting room
		GameFlags = new Dictionary<string, string>();
	}
}