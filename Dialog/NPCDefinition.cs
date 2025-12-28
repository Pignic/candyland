using System.Collections.Generic;

namespace EldmeresTale.Dialog;

public class NPCDefinition {
	public string id { get; set; }
	public string nameKey { get; set; }  // Localization key for NPC name
	public string defaultPortrait { get; set; }  // Default portrait sprite
	public string requiresItem { get; set; }  // Item required to talk to this NPC
	public string refuseDialogKey { get; set; }  // What NPC says if requirement not met
	public List<NPCDialogEntry> dialogs { get; set; }

	public NPCDefinition() {
		this.dialogs = new List<NPCDialogEntry>();
	}
}

public class NPCDialogEntry {
	public string treeId { get; set; }  // Which dialog tree to use
	public int priority { get; set; }  // Lower number = higher priority
	public List<string> conditions { get; set; }  // Conditions that must be met

	public NPCDialogEntry() {
		this.conditions = new List<string>();
		this.priority = 999;  // Default low priority
	}
}