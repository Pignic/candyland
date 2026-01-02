using System.Collections.Generic;

namespace EldmeresTale.Dialog;

/// <summary>
/// A command node that executes cutscene commands instead of showing dialog
/// </summary>
public class CommandNode : DialogItem {
	public CutsceneCommand command { get; set; }
	public string nextNodeId { get; set; }

	public CommandNode() : base() {
	}
}