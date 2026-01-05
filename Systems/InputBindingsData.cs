using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class InputBindingsData {
	public Dictionary<string, ActionBindings> Bindings { get; set; }

	public InputBindingsData() {
		Bindings = new Dictionary<string, ActionBindings>();
	}
}

public class ActionBindings {
	public List<string> Keyboard { get; set; }
	public List<string> Mouse { get; set; }
	public List<string> Gamepad { get; set; }

	public ActionBindings() {
		Keyboard = new List<string>();
		Mouse = new List<string>();
		Gamepad = new List<string>();
	}
}
