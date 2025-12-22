using System.Collections.Generic;

namespace Candyland.Systems;

public class InputBindingsData {
	public Dictionary<string, ActionBindings> bindings { get; set; }

	public InputBindingsData() {
		bindings = new Dictionary<string, ActionBindings>();
	}
}

public class ActionBindings {
	public List<string> keyboard { get; set; }
	public List<string> mouse { get; set; }
	public List<string> gamepad { get; set; }

	public ActionBindings() {
		keyboard = new List<string>();
		mouse = new List<string>();
		gamepad = new List<string>();
	}
}