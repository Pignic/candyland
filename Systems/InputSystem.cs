using EldmeresTale.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EldmeresTale.Systems;

public class InputSystem : GameSystem {
	// Input state
	private KeyboardState _currentKeyState;
	private KeyboardState _previousKeyState;
	private MouseState _currentMouseState;
	private MouseState _previousMouseState;
	private GamePadState _currentGamePadState;
	private GamePadState _previousGamePadState;

	// Bindings (action -> list of inputs)
	private Dictionary<GameAction, List<Keys>> _keyboardBindings;
	private Dictionary<GameAction, List<MouseButton>> _mouseBindings;
	private Dictionary<GameAction, List<Buttons>> _gamepadBindings;

	// Settings
	private const float GAMEPAD_DEADZONE = 0.2f;
	private const string DEFAULT_BINDINGS_PATH = "Assets/Input/bindings.json";

	// Graphics device (for mouse coordinate conversion)
	private readonly GraphicsDevice _graphicsDevice;

	public InputSystem(GraphicsDevice graphicsDevice) {
		_graphicsDevice = graphicsDevice;
		_keyboardBindings = new Dictionary<GameAction, List<Keys>>();
		_mouseBindings = new Dictionary<GameAction, List<MouseButton>>();
		_gamepadBindings = new Dictionary<GameAction, List<Buttons>>();

		Enabled = true;
		Visible = false;
	}

	public override void Initialize() {
		// Try to load bindings from JSON
		bool loaded = LoadBindings(DEFAULT_BINDINGS_PATH);

		if(!loaded) {
			// Fallback to hardcoded defaults
			System.Diagnostics.Debug.WriteLine("[INPUT SYSTEM] Failed to load bindings, using defaults");
			SetDefaultBindings();
		}

		System.Diagnostics.Debug.WriteLine("[INPUT SYSTEM] Initialized");
	}

	public override void Update(GameTime time) {
		// Update input states
		_previousKeyState = _currentKeyState;
		_previousMouseState = _currentMouseState;
		_previousGamePadState = _currentGamePadState;

		_currentKeyState = Keyboard.GetState();
		_currentMouseState = Mouse.GetState();
		_currentGamePadState = GamePad.GetState(PlayerIndex.One);
	}

	public KeyboardState GetKeyboardStateState() {
		return _currentKeyState;
	}

	public KeyboardState GetPreviousKeyboardStateState() {
		return _previousKeyState;
	}

	public InputCommands GetCommands(Camera camera = null) {
		var commands = new InputCommands();

		// Movement
		commands.Movement = GetMovementVector();
		commands.MoveUpPressed = IsActionPressed(GameAction.MoveUp);
		commands.MoveDownPressed = IsActionPressed(GameAction.MoveDown);
		commands.MoveLeftPressed = IsActionPressed(GameAction.MoveLeft);
		commands.MoveRightPressed = IsActionPressed(GameAction.MoveRight);

		// Actions - Pressed, Held, Released
		commands.InteractPressed = IsActionPressed(GameAction.Interact);
		commands.InteractHeld = IsActionHeld(GameAction.Interact);
		commands.InteractReleased = IsActionReleased(GameAction.Interact);

		commands.AttackPressed = IsActionPressed(GameAction.Attack);
		commands.AttackHeld = IsActionHeld(GameAction.Attack);
		commands.AttackReleased = IsActionReleased(GameAction.Attack);

		commands.DodgePressed = IsActionPressed(GameAction.Dodge);
		commands.DodgeHeld = IsActionHeld(GameAction.Dodge);
		commands.DodgeReleased = IsActionReleased(GameAction.Dodge);

		commands.MenuPressed = IsActionPressed(GameAction.Menu);
		commands.MenuHeld = IsActionHeld(GameAction.Menu);
		commands.MenuReleased = IsActionReleased(GameAction.Menu);

		commands.CancelPressed = IsActionPressed(GameAction.Cancel);
		commands.CancelHeld = IsActionHeld(GameAction.Cancel);
		commands.CancelReleased = IsActionReleased(GameAction.Cancel);

		// Mouse position
		commands.MouseScreenPosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);

		// Convert to world position if camera provided
		if(camera != null) {
			commands.MouseWorldPosition = camera.ScreenToWorld(commands.MouseScreenPosition);
		} else {
			commands.MouseWorldPosition = commands.MouseScreenPosition;
		}

		// Mouse buttons
		commands.MouseLeftPressed = IsMouseButtonPressed(MouseButton.Left);
		commands.MouseLeftHeld = IsMouseButtonHeld(MouseButton.Left);
		commands.MouseLeftReleased = IsMouseButtonReleased(MouseButton.Left);

		commands.MouseRightPressed = IsMouseButtonPressed(MouseButton.Right);
		commands.MouseRightHeld = IsMouseButtonHeld(MouseButton.Right);
		commands.MouseRightReleased = IsMouseButtonReleased(MouseButton.Right);

		commands.MouseMiddlePressed = IsMouseButtonPressed(MouseButton.Middle);
		commands.MouseMiddleHeld = IsMouseButtonHeld(MouseButton.Middle);
		commands.MouseMiddleReleased = IsMouseButtonReleased(MouseButton.Middle);

		// Debug commands
		commands.ToggleDebugMode = IsActionPressed(GameAction.ToggleDebugMode);
		commands.MapEditor = IsActionPressed(GameAction.MapEditor);

		return commands;
	}

	// ACTION QUERIES
	public bool IsActionPressed(GameAction action) {
		// Check keyboard
		if(_keyboardBindings.ContainsKey(action)) {
			foreach(var key in _keyboardBindings[action]) {
				if(IsKeyPressed(key)) return true;
			}
		}

		// Check mouse
		if(_mouseBindings.ContainsKey(action)) {
			foreach(var button in _mouseBindings[action]) {
				if(IsMouseButtonPressed(button)) return true;
			}
		}

		// Check gamepad
		if(_gamepadBindings.ContainsKey(action)) {
			foreach(var button in _gamepadBindings[action]) {
				if(IsGamePadButtonPressed(button)) return true;
			}
		}

		return false;
	}

	public bool IsActionHeld(GameAction action) {
		// Check keyboard
		if(_keyboardBindings.ContainsKey(action)) {
			foreach(var key in _keyboardBindings[action]) {
				if(IsKeyHeld(key)) return true;
			}
		}

		// Check mouse
		if(_mouseBindings.ContainsKey(action)) {
			foreach(var button in _mouseBindings[action]) {
				if(IsMouseButtonHeld(button)) return true;
			}
		}

		// Check gamepad
		if(_gamepadBindings.ContainsKey(action)) {
			foreach(var button in _gamepadBindings[action]) {
				if(IsGamePadButtonHeld(button)) return true;
			}
		}

		return false;
	}

	public bool IsActionReleased(GameAction action) {
		// Check keyboard
		if(_keyboardBindings.ContainsKey(action)) {
			foreach(var key in _keyboardBindings[action]) {
				if(IsKeyReleased(key)) return true;
			}
		}

		// Check mouse
		if(_mouseBindings.ContainsKey(action)) {
			foreach(var button in _mouseBindings[action]) {
				if(IsMouseButtonReleased(button)) return true;
			}
		}

		// Check gamepad
		if(_gamepadBindings.ContainsKey(action)) {
			foreach(var button in _gamepadBindings[action]) {
				if(IsGamePadButtonReleased(button)) return true;
			}
		}

		return false;
	}

	// MOVEMENT
	private Vector2 GetMovementVector() {
		Vector2 movement = Vector2.Zero;

		// Keyboard movement
		if(IsActionHeld(GameAction.MoveUp)) movement.Y -= 1;
		if(IsActionHeld(GameAction.MoveDown)) movement.Y += 1;
		if(IsActionHeld(GameAction.MoveLeft)) movement.X -= 1;
		if(IsActionHeld(GameAction.MoveRight)) movement.X += 1;

		// Gamepad left stick (analog)
		if(_currentGamePadState.IsConnected) {
			Vector2 leftStick = _currentGamePadState.ThumbSticks.Left;

			// Apply deadzone
			if(leftStick.Length() > GAMEPAD_DEADZONE) {
				// Invert Y (gamepad Y is inverted by default)
				movement = new Vector2(leftStick.X, -leftStick.Y);
			}
		}

		// Normalize diagonal movement (keyboard only - gamepad is already normalized)
		if(movement != Vector2.Zero && movement.Length() > 1f) {
			movement.Normalize();
		}

		return movement;
	}

	// RAW INPUT CHECKS
	private bool IsKeyPressed(Keys key) =>
		_currentKeyState.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);

	private bool IsKeyHeld(Keys key) =>
		_currentKeyState.IsKeyDown(key);

	private bool IsKeyReleased(Keys key) =>
		_currentKeyState.IsKeyUp(key) && _previousKeyState.IsKeyDown(key);

	private bool IsMouseButtonPressed(MouseButton button) {
		return button switch {
			MouseButton.Left => _currentMouseState.LeftButton == ButtonState.Pressed &&
								_previousMouseState.LeftButton == ButtonState.Released,
			MouseButton.Right => _currentMouseState.RightButton == ButtonState.Pressed &&
								 _previousMouseState.RightButton == ButtonState.Released,
			MouseButton.Middle => _currentMouseState.MiddleButton == ButtonState.Pressed &&
								  _previousMouseState.MiddleButton == ButtonState.Released,
			_ => false
		};
	}

	private bool IsMouseButtonHeld(MouseButton button) {
		return button switch {
			MouseButton.Left => _currentMouseState.LeftButton == ButtonState.Pressed,
			MouseButton.Right => _currentMouseState.RightButton == ButtonState.Pressed,
			MouseButton.Middle => _currentMouseState.MiddleButton == ButtonState.Pressed,
			_ => false
		};
	}

	private bool IsMouseButtonReleased(MouseButton button) {
		return button switch {
			MouseButton.Left => _currentMouseState.LeftButton == ButtonState.Released &&
								_previousMouseState.LeftButton == ButtonState.Pressed,
			MouseButton.Right => _currentMouseState.RightButton == ButtonState.Released &&
								 _previousMouseState.RightButton == ButtonState.Pressed,
			MouseButton.Middle => _currentMouseState.MiddleButton == ButtonState.Released &&
								  _previousMouseState.MiddleButton == ButtonState.Pressed,
			_ => false
		};
	}

	private bool IsGamePadButtonPressed(Buttons button) =>
		_currentGamePadState.IsButtonDown(button) && _previousGamePadState.IsButtonUp(button);

	private bool IsGamePadButtonHeld(Buttons button) =>
		_currentGamePadState.IsButtonDown(button);

	private bool IsGamePadButtonReleased(Buttons button) =>
		_currentGamePadState.IsButtonUp(button) && _previousGamePadState.IsButtonDown(button);

	// BINDING MANAGEMENT
	public bool LoadBindings(string path) {
		try {
			if(!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[INPUT SYSTEM] Bindings file not found: {path}");
				return false;
			}

			string json = File.ReadAllText(path);
			var data = JsonSerializer.Deserialize<InputBindingsData>(json);

			if(data == null || data.bindings == null) {
				System.Diagnostics.Debug.WriteLine("[INPUT SYSTEM] Invalid bindings data");
				return false;
			}

			// Clear existing bindings
			_keyboardBindings.Clear();
			_mouseBindings.Clear();
			_gamepadBindings.Clear();

			// Load each action's bindings
			foreach(var kvp in data.bindings) {
				// Try to parse action name to enum
				if(!Enum.TryParse<GameAction>(kvp.Key, true, out var action)) {
					System.Diagnostics.Debug.WriteLine($"[INPUT SYSTEM] Unknown action: {kvp.Key}");
					continue;
				}

				var bindings = kvp.Value;

				// Load keyboard bindings
				if(bindings.keyboard != null) {
					var keyList = new List<Keys>();
					foreach(var keyName in bindings.keyboard) {
						if(Enum.TryParse<Keys>(keyName, true, out var key)) {
							keyList.Add(key);
						}
					}
					if(keyList.Count > 0) {
						_keyboardBindings[action] = keyList;
					}
				}

				// Load mouse bindings
				if(bindings.mouse != null) {
					var mouseList = new List<MouseButton>();
					foreach(var buttonName in bindings.mouse) {
						if(Enum.TryParse<MouseButton>(buttonName, true, out var button)) {
							mouseList.Add(button);
						}
					}
					if(mouseList.Count > 0) {
						_mouseBindings[action] = mouseList;
					}
				}

				// Load gamepad bindings
				if(bindings.gamepad != null) {
					var buttonList = new List<Buttons>();
					foreach(var buttonName in bindings.gamepad) {
						if(Enum.TryParse<Buttons>(buttonName, true, out var button)) {
							buttonList.Add(button);
						}
					}
					if(buttonList.Count > 0) {
						_gamepadBindings[action] = buttonList;
					}
				}
			}

			System.Diagnostics.Debug.WriteLine($"[INPUT SYSTEM] Loaded bindings from {path}");
			return true;

		} catch(Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[INPUT SYSTEM] Error loading bindings: {ex.Message}");
			return false;
		}
	}

	public void SaveBindings(string path = DEFAULT_BINDINGS_PATH) {
		try {
			var data = new InputBindingsData();

			// Convert bindings to JSON structure
			foreach(var action in Enum.GetValues<GameAction>()) {
				var bindings = new ActionBindings();

				// Keyboard
				if(_keyboardBindings.ContainsKey(action)) {
					bindings.keyboard = _keyboardBindings[action]
						.Select(k => k.ToString())
						.ToList();
				}

				// Mouse
				if(_mouseBindings.ContainsKey(action)) {
					bindings.mouse = _mouseBindings[action]
						.Select(b => b.ToString())
						.ToList();
				}

				// Gamepad
				if(_gamepadBindings.ContainsKey(action)) {
					bindings.gamepad = _gamepadBindings[action]
						.Select(b => b.ToString())
						.ToList();
				}

				// Only save if action has bindings
				if(bindings.keyboard.Count > 0 || bindings.mouse.Count > 0 || bindings.gamepad.Count > 0) {
					data.bindings[action.ToString().ToLower()] = bindings;
				}
			}

			// Serialize to JSON
			var options = new JsonSerializerOptions {
				WriteIndented = true
			};
			string json = JsonSerializer.Serialize(data, options);

			// Ensure directory exists
			string directory = Path.GetDirectoryName(path);
			if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
			}

			File.WriteAllText(path, json);
			System.Diagnostics.Debug.WriteLine($"[INPUT SYSTEM] Saved bindings to {path}");

		} catch(Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[INPUT SYSTEM] Error saving bindings: {ex.Message}");
		}
	}

	// Reset to hardcoded default bindings
	public void ResetToDefaults() {
		SetDefaultBindings();
		System.Diagnostics.Debug.WriteLine("[INPUT SYSTEM] Reset to default bindings");
	}

	// Set hardcoded default bindings (fallback if JSON missing)
	private void SetDefaultBindings() {
		_keyboardBindings.Clear();
		_mouseBindings.Clear();
		_gamepadBindings.Clear();

		// Gameplay actions
		_keyboardBindings[GameAction.Interact] = new() { Keys.E, Keys.Enter };
		_keyboardBindings[GameAction.Attack] = new() { Keys.Space };
		_keyboardBindings[GameAction.Attack] = new() { Keys.LeftShift, Keys.RightShift };
		_keyboardBindings[GameAction.Menu] = new() { Keys.Tab };
		_keyboardBindings[GameAction.Cancel] = new() { Keys.Escape };

		// Movement
		_keyboardBindings[GameAction.MoveUp] = new() { Keys.W, Keys.Up };
		_keyboardBindings[GameAction.MoveDown] = new() { Keys.S, Keys.Down };
		_keyboardBindings[GameAction.MoveLeft] = new() { Keys.A, Keys.Left };
		_keyboardBindings[GameAction.MoveRight] = new() { Keys.D, Keys.Right };

		// Tab switching
		_keyboardBindings[GameAction.TabLeft] = new() { Keys.Q };
		_keyboardBindings[GameAction.TabRight] = new() { Keys.E };

		// Mouse bindings
		_mouseBindings[GameAction.Interact] = new() { MouseButton.Left };
		_mouseBindings[GameAction.Cancel] = new() { MouseButton.Right };

		// Gamepad bindings
		_gamepadBindings[GameAction.Interact] = new() { Buttons.A };
		_gamepadBindings[GameAction.Attack] = new() { Buttons.X };
		_gamepadBindings[GameAction.Dodge] = new() { Buttons.B };
		_gamepadBindings[GameAction.Menu] = new() { Buttons.Start };
		_gamepadBindings[GameAction.Cancel] = new() { Buttons.B };
		_gamepadBindings[GameAction.TabLeft] = new() { Buttons.LeftShoulder };
		_gamepadBindings[GameAction.TabRight] = new() { Buttons.RightShoulder };

		// Debug actions
		_keyboardBindings[GameAction.ToggleDebugMode] = new() { Keys.F4 };
		_keyboardBindings[GameAction.MapEditor] = new() { Keys.M };
	}

	public void RebindAction(GameAction action, InputDevice device, string inputName) {
		switch(device) {
			case InputDevice.Keyboard:
				if(Enum.TryParse<Keys>(inputName, true, out var key)) {
					if(!_keyboardBindings.ContainsKey(action)) {
						_keyboardBindings[action] = new List<Keys>();
					}
					if(!_keyboardBindings[action].Contains(key)) {
						_keyboardBindings[action].Add(key);
					}
				}
				break;

			case InputDevice.Mouse:
				if(Enum.TryParse<MouseButton>(inputName, true, out var mouseButton)) {
					if(!_mouseBindings.ContainsKey(action)) {
						_mouseBindings[action] = new List<MouseButton>();
					}
					if(!_mouseBindings[action].Contains(mouseButton)) {
						_mouseBindings[action].Add(mouseButton);
					}
				}
				break;

			case InputDevice.Gamepad:
				if(Enum.TryParse<Buttons>(inputName, true, out var button)) {
					if(!_gamepadBindings.ContainsKey(action)) {
						_gamepadBindings[action] = new List<Buttons>();
					}
					if(!_gamepadBindings[action].Contains(button)) {
						_gamepadBindings[action].Add(button);
					}
				}
				break;
		}
	}

	public List<string> GetBindingsForAction(GameAction action, InputDevice device) {
		return device switch {
			InputDevice.Keyboard => _keyboardBindings.ContainsKey(action)
				? _keyboardBindings[action].Select(k => k.ToString()).ToList()
				: new List<string>(),

			InputDevice.Mouse => _mouseBindings.ContainsKey(action)
				? _mouseBindings[action].Select(b => b.ToString()).ToList()
				: new List<string>(),

			InputDevice.Gamepad => _gamepadBindings.ContainsKey(action)
				? _gamepadBindings[action].Select(b => b.ToString()).ToList()
				: new List<string>(),

			_ => new List<string>()
		};
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// InputSystem doesn't draw anything
	}

	public override void Dispose() {
		System.Diagnostics.Debug.WriteLine("[INPUT SYSTEM] Disposed");
	}
}