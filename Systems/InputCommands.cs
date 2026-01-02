using Microsoft.Xna.Framework;

namespace EldmeresTale.Systems;

public struct InputCommands {

	// MOVEMENT (Analog, continuous)
	public Vector2 Movement;

	public bool MoveUpPressed;
	public bool MoveDownPressed;
	public bool MoveLeftPressed;
	public bool MoveRightPressed;

	// ACTIONS - Pressed (triggers once per keypress)
	public bool InteractPressed;
	public bool AttackPressed;
	public bool MenuPressed;
	public bool CancelPressed;

	// ACTIONS - Held (continuous, true while held down)
	public bool InteractHeld;
	public bool AttackHeld;
	public bool MenuHeld;
	public bool CancelHeld;

	// ACTIONS - Released (triggers once when button released)
	public bool InteractReleased;
	public bool AttackReleased;
	public bool MenuReleased;
	public bool CancelReleased;

	// MOUSE
	public Vector2 MouseScreenPosition;
	public Vector2 MouseWorldPosition;

	// Mouse buttons - Pressed (once per click)
	public bool MouseLeftPressed;
	public bool MouseRightPressed;
	public bool MouseMiddlePressed;

	// Mouse buttons - Held (continuous)
	public bool MouseLeftHeld;
	public bool MouseRightHeld;
	public bool MouseMiddleHeld;

	// Mouse buttons - Released (once per release)
	public bool MouseLeftReleased;
	public bool MouseRightReleased;
	public bool MouseMiddleReleased;

	// Debug
	public bool MapEditor;
	public bool ToggleDebugMode;

	// HELPER METHODS
	public bool IsMoving => Movement.LengthSquared() > 0;

	public bool AnyActionPressed =>
		InteractPressed || AttackPressed || MenuPressed || CancelPressed ||
		MouseLeftPressed || MouseRightPressed || MouseMiddlePressed;
}