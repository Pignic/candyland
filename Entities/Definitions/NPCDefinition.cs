using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EldmeresTale.Entities.Definitions;

public class NPCDefinition {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("nameKey")]
	public string NameKey { get; set; }  // Localization key for NPC name

	[JsonPropertyName("defaultPortrait")]
	public string DefaultPortrait { get; set; }  // Default portrait sprite

	[JsonPropertyName("requiresItem")]
	public string RequiresItem { get; set; }  // Item required to talk to this NPC

	[JsonPropertyName("refuseDialogKey")]
	public string RefuseDialogKey { get; set; }  // What NPC says if requirement not met

	[JsonPropertyName("dialogs")]
	public List<NPCDialogEntry> Dialogs { get; set; }

	public NPCDefinition() {
		Dialogs = [];
	}
}

public class NPCDialogEntry {

	[JsonPropertyName("treeId")]
	public string TreeId { get; set; }  // Which dialog tree to use

	[JsonPropertyName("priority")]
	public int Priority { get; set; }  // Lower number = higher priority

	[JsonPropertyName("conditions")]
	public List<string> Conditions { get; set; }  // Conditions that must be met

	public NPCDialogEntry() {
		Conditions = [];
		Priority = 999;  // Default low priority
	}
}