namespace EldmeresTale.Systems;

public enum GameAction {
	// Gameplay actions
	Interact,
	Attack,
	Dodge,
	Menu,
	Cancel,
	MoveUp,
	MoveDown,
	MoveLeft,
	MoveRight,

	// UI navigation
	TabLeft,
	TabRight,

	// Debug actions (only available in DEBUG builds)
	ToggleDebugMode,
	MapEditor,
	QuickSave,
	QuickLoad
}

public enum InputDevice {
	Keyboard,
	Mouse,
	Gamepad
}

public enum MouseButton {
	Left,
	Right,
	Middle
}