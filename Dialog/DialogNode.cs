using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EldmeresTale.Dialog;

public abstract class DialogItem {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("textKey")]
	public string TextKey { get; set; }  // Localization key for response text

	[JsonPropertyName("effects")]
	public List<string> Effects { get; set; }  // Effects to execute when this node is shown

	public DialogItem() {
		Effects = [];
	}

}

public class DialogNode : DialogItem {

	[JsonPropertyName("nodeType")]
	public string NodeType { get; set; } = "dialog"; // "dialog" or "command"

	[JsonPropertyName("portraitKey")]
	public string PortraitKey { get; set; }

	[JsonPropertyName("responses")]
	public List<DialogResponse> Responses { get; set; }

	[JsonPropertyName("isEndNode")]
	public bool IsEndNode { get; set; }

	[JsonPropertyName("command")]
	public CutsceneCommand Command { get; set; }

	[JsonPropertyName("nextNodeId")]
	public string NextNodeId { get; set; } // For command nodes

	public DialogNode() : base() {
		Responses = [];
		IsEndNode = false;
	}

	public bool IsCommand() => NodeType == "command";
	public bool IsDialog() => NodeType == "dialog";
}

public class DialogResponse : DialogItem {

	[JsonPropertyName("nextNodeId")]
	public string NextNodeId { get; set; }  // Which node to go to

	[JsonPropertyName("conditions")]
	public List<string> Conditions { get; set; }  // Conditions that must be met to show this option

	public DialogResponse() : base() {
		Conditions = [];
	}
}