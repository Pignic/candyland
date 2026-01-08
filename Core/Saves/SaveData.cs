using System;

namespace EldmeresTale.Core.Saves;

public class SaveData {

	public int Version { get; set; } = 1;

	public PlayerSaveData Player { get; set; }

	public QuestSaveData Quests { get; set; }

	public WorldSaveData World { get; set; }

	public DateTime SaveTime { get; set; }

	public string SaveName { get; set; }

	public SaveData() {
		Player = new PlayerSaveData();
		Quests = new QuestSaveData();
		World = new WorldSaveData();
		SaveTime = DateTime.Now;
		SaveName = "save1";
	}
}