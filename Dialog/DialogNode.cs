using System.Collections.Generic;

namespace Candyland.Dialog;

public abstract class DialogItem {
	public string id { get; set; }
	public string textKey { get; set; }  // Localization key for response text
	public List<string> effects { get; set; }  // Effects to execute when this node is shown

	public DialogItem() {
		effects = new List<string>();
	}

}

public class DialogNode : DialogItem {
	public string portraitKey { get; set; }  // Which portrait to show
	public List<DialogResponse> responses { get; set; }
	public bool isEndNode { get; set; }  // True if this ends the conversation

	public DialogNode() : base() {
		this.responses = new List<DialogResponse>();
		this.isEndNode = false;
	}
}

public class DialogResponse : DialogItem {
	public string nextNodeId { get; set; }  // Which node to go to
	public List<string> conditions { get; set; }  // Conditions that must be met to show this option

	public DialogResponse() : base() {
		conditions = new List<string>();
	}
}