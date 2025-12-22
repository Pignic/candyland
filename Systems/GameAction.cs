namespace Candyland.Systems;

public enum GameAction {
	// Gameplay actions
	Interact,
	Attack,
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
	DebugQuest1,
	DebugQuest2,
	DebugQuest3,
	MapEditor
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