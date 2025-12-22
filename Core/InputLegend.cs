using Candyland.Core.UI;
using Candyland.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Candyland.Core;

public class InputLegend {
	private readonly InputSystem _inputSystem;
	private readonly BitmapFont _font;

	public InputDevice ActiveDevice { get; private set; }

	private readonly Dictionary<InputDevice, double> _lastInputTime;
	private const double INPUT_TIMEOUT = 2.0; // Seconds before switching device

	public InputLegend(InputSystem inputSystem, BitmapFont font) {
		_inputSystem = inputSystem;
		_font = font;
		ActiveDevice = InputDevice.Keyboard; // Default to keyboard

		_lastInputTime = new Dictionary<InputDevice, double> {
			{ InputDevice.Keyboard, 0 },
			{ InputDevice.Mouse, 0 },
			{ InputDevice.Gamepad, 0 }
		};
	}

	public void Update(InputCommands input, GameTime gameTime) {
		double currentTime = gameTime.TotalGameTime.TotalSeconds;

		// Detect keyboard input
		if(input.Movement != Vector2.Zero ||
		   input.InteractPressed || input.AttackPressed ||
		   input.MenuPressed || input.CancelPressed) {

			// Check if it's from keyboard (not gamepad stick)
			// Simple heuristic: if movement is exactly -1, 0, or 1, likely keyboard
			if(input.Movement.X == 0 || Math.Abs(input.Movement.X) == 1) {
				_lastInputTime[InputDevice.Keyboard] = currentTime;
			}
		}

		// Detect mouse input
		if(input.MouseLeftPressed || input.MouseRightPressed ||
		   input.MouseLeftHeld || input.MouseRightHeld) {
			_lastInputTime[InputDevice.Mouse] = currentTime;
		}

		// Detect gamepad input (analog stick or any button)
		if(input.Movement != Vector2.Zero &&
		   input.Movement.X != 0 && Math.Abs(input.Movement.X) != 1) {
			// Analog values that aren't exactly -1, 0, 1 = gamepad
			_lastInputTime[InputDevice.Gamepad] = currentTime;
		}

		// Update active device (most recently used)
		double mostRecentTime = 0;
		foreach(var kvp in _lastInputTime) {
			if(kvp.Value > mostRecentTime) {
				mostRecentTime = kvp.Value;
				ActiveDevice = kvp.Key;
			}
		}
	}

	public string GetActionText(GameAction action) {
		var bindings = _inputSystem.GetBindingsForAction(action, ActiveDevice);

		if(bindings.Count == 0) {
			return "?";
		}

		// Get first binding
		string binding = bindings[0];

		// Format for display
		if(ActiveDevice == InputDevice.Gamepad) {
			// Gamepad: wrap in parentheses
			return $"({FormatGamepadButton(binding)})";
		} else if(ActiveDevice == InputDevice.Mouse) {
			// Mouse: show as-is
			return FormatMouseButton(binding);
		} else {
			// Keyboard: show as-is
			return binding;
		}
	}

	public string GetMultipleActionsText(params GameAction[] actions) {
		var texts = actions.Select(a => GetActionText(a)).ToArray();
		return string.Join("/", texts);
	}

	private string FormatGamepadButton(string button) {
		return button switch {
			"LeftShoulder" => "LB",
			"RightShoulder" => "RB",
			"LeftTrigger" => "LT",
			"RightTrigger" => "RT",
			"DPadUp" => "D-Up",
			"DPadDown" => "D-Down",
			"DPadLeft" => "D-Left",
			"DPadRight" => "D-Right",
			_ => button
		};
	}

	private string FormatMouseButton(string button) {
		return button switch {
			"Left" => "LMB",
			"Right" => "RMB",
			"Middle" => "MMB",
			_ => button
		};
	}

	public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight,
					 params (GameAction action, string label)[] entries) {

		if(entries.Length == 0) return;

		// Build legend text
		var legendParts = new List<string>();
		foreach(var (action, label) in entries) {
			string actionText = GetActionText(action);
			legendParts.Add($"{actionText} - {label}");
		}

		string legendText = string.Join("    ", legendParts);

		// Measure text
		Vector2 textSize = _font.getSize(legendText);

		// Position at bottom center
		int x = (screenWidth - (int)textSize.X) / 2;
		int y = screenHeight - (int)textSize.Y - 10;

		// Draw background
		var bgRect = new Rectangle(
			x - 5,
			y - 3,
			(int)textSize.X + 10,
			(int)textSize.Y + 6
		);

		// Semi-transparent black background
		Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
		pixel.SetData(new[] { Color.Black });
		spriteBatch.Draw(pixel, bgRect, Color.Black * 0.7f);

		// Draw text
		_font.drawText(spriteBatch, legendText, new Vector2(x, y), Color.White);
	}

	public void DrawAt(SpriteBatch spriteBatch, Vector2 position,
					   params (GameAction action, string label)[] entries) {

		if(entries.Length == 0) return;

		// Build legend text
		var legendParts = new List<string>();
		foreach(var (action, label) in entries) {
			string actionText = GetActionText(action);
			legendParts.Add($"{actionText} - {label}");
		}

		string legendText = string.Join("    ", legendParts);

		// Measure text
		Vector2 textSize = _font.getSize(legendText);

		// Draw background
		var bgRect = new Rectangle(
			(int)position.X - 5,
			(int)position.Y - 3,
			(int)textSize.X + 10,
			(int)textSize.Y + 6
		);

		Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
		pixel.SetData(new[] { Color.Black });
		spriteBatch.Draw(pixel, bgRect, Color.Black * 0.7f);

		// Draw text
		_font.drawText(spriteBatch, legendText, position, Color.White);
	}

	public void SetActiveDevice(InputDevice device) {
		ActiveDevice = device;
	}
}