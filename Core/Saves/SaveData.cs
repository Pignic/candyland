using System;

namespace EldmeresTale.Core.Saves;

public class SaveData {
	/// <summary>
	/// Save format version - increment when making breaking changes to save format
	/// </summary>
	public int Version { get; set; } = 1;

	/// <summary>
	/// Player state (stats, inventory, position, etc.)
	/// </summary>
	public PlayerSaveData Player { get; set; }

	/// <summary>
	/// Quest progress and completion state
	/// </summary>
	public QuestSaveData Quests { get; set; }

	/// <summary>
	/// World state (current room, game flags, etc.)
	/// </summary>
	public WorldSaveData World { get; set; }

	/// <summary>
	/// When this save was created
	/// </summary>
	public DateTime SaveTime { get; set; }

	/// <summary>
	/// Save slot name (e.g., "save1", "quicksave")
	/// </summary>
	public string SaveName { get; set; }

	public SaveData() {
		Player = new PlayerSaveData();
		Quests = new QuestSaveData();
		World = new WorldSaveData();
		SaveTime = DateTime.Now;
		SaveName = "save1";
	}
}