using System.Text.Json.Serialization;

namespace EldmeresTale.Dialog;

public class CommandNode : DialogItem {

	[JsonPropertyName("command")]
	public CutsceneCommand Command { get; set; }

	[JsonPropertyName("nextNodeId")]
	public string NextNodeId { get; set; }

}