using System.Collections.Generic;

namespace EldmeresTale.Core.Saves;

public class WorldSaveData {
	public string CurrentRoomId { get; set; }

	public Dictionary<string, string> GameFlags { get; set; }

	public WorldSaveData() {
		CurrentRoomId = "room1";  // Default starting room
		GameFlags = [];
	}
}