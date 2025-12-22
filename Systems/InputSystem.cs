using Microsoft.Xna.Framework.Input;

namespace Candyland.Systems;

public class InputSystem : GameSystem {
	private KeyboardState _previousKeyState;
	private KeyboardState _currentKeyState;

	//public InputCommands GetCommands() {
	//	return new InputCommands {
	//		Movement = GetMovementVector(),
	//		Attack = IsAttackPressed(),
	//		Interact = IsInteractPressed(),
	//		OpenMenu = IsMenuPressed(),
	//		OpenMapEditor = IsMapEditorPressed(),
	//		// Debug commands
	//		DebugStartQuest = GetDebugQuestCommand(),
	//	};
	//}

	//private Vector2 GetMovementVector() {
	//	// TODO
	//}
	//private bool IsAttackPressed() {
	//	// TODO
	//}
}