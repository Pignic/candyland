using EldmeresTale.Dialog;
using EldmeresTale.Entities.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIDialog {
	private readonly DialogManager _dialogManager;

	// UI hierarchy
	private UIPanel _rootPanel;
	private UIPanel _dialogBoxPanel;
	private UIPanel _responsePanel;
	private List<UIButton> _responseButtons;

	private MouseState _previousMouseState;


	// Custom UI elements
	private UIDialogText _dialogText;
	private UIPortrait _portrait;

	// State
	private string _currentNodeId = "";
	private int _selectedResponseIndex = 0;
	private int _scale;

	// Input tracking
	private KeyboardState _previousKeyState;

	public bool IsActive => _dialogManager.IsDialogActive;

	public event Action OnResponseChosen;

	public void SetScale(int newScale) {
		_scale = newScale;
	}

	public UIDialog(DialogManager dialogManager, int screenWidth, int screenHeight, int scale) {
		_dialogManager = dialogManager;
		_scale = scale;
		BuildUI(screenWidth, screenHeight);

		_previousMouseState = Mouse.GetState();
	}
	private MouseState ScaleMouseState(MouseState original) {
		Point scaledPosition = new Point(
			original.Position.X / _scale,
			original.Position.Y / _scale
		);

		return new MouseState(
			scaledPosition.X,
			scaledPosition.Y,
			original.ScrollWheelValue,
			original.LeftButton,
			original.MiddleButton,
			original.RightButton,
			original.XButton1,
			original.XButton2
		);
	}

	private void BuildUI(int screenWidth, int screenHeight) {
		const int DIALOG_HEIGHT = 120;
		const int MARGIN = 10;
		const int PORTRAIT_SIZE = 60;
		const int PORTRAIT_MARGIN = 5;

		// === ROOT PANEL ===
		_rootPanel = new UIPanel() {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = Color.Transparent,
			Visible = false
		};

		// === DIALOG BOX PANEL (bottom of screen) ===
		_dialogBoxPanel = new UIPanel() {
			X = MARGIN,
			Y = screenHeight - DIALOG_HEIGHT - MARGIN,
			Width = screenWidth - (MARGIN * 2),
			Height = DIALOG_HEIGHT,
			BackgroundColor = new Color(0, 0, 0, 200),
			BorderColor = Color.White,
			BorderWidth = 3
		};
		_rootPanel.AddChild(_dialogBoxPanel);

		// === PORTRAIT (left side of dialog box) ===
		_portrait = new UIPortrait() {
			X = PORTRAIT_MARGIN,
			Y = PORTRAIT_MARGIN,
			Width = PORTRAIT_SIZE,
			Height = PORTRAIT_SIZE
		};
		_dialogBoxPanel.AddChild(_portrait);

		// === DIALOG TEXT (with typewriter and NPC name) ===
		const int textX = PORTRAIT_SIZE + (PORTRAIT_MARGIN * 2);
		int textWidth = _dialogBoxPanel.Width - textX - PORTRAIT_MARGIN;

		_dialogText = new UIDialogText() {
			X = textX,
			Y = PORTRAIT_MARGIN,
			Width = textWidth,
			Height = PORTRAIT_SIZE
		};
		_dialogBoxPanel.AddChild(_dialogText);

		// === RESPONSE PANEL (below dialog text) ===
		_responsePanel = new UIPanel() {
			X = textX,
			Y = PORTRAIT_SIZE + PORTRAIT_MARGIN + 5,
			Width = textWidth,
			Height = DIALOG_HEIGHT - PORTRAIT_SIZE - (PORTRAIT_MARGIN * 2) - 5,
			BackgroundColor = Color.Transparent,
			BorderColor = Color.Transparent,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 2
		};
		_dialogBoxPanel.AddChild(_responsePanel);

		_responseButtons = [];
		_previousKeyState = Keyboard.GetState();
	}

	public void LoadPortrait(string key, Texture2D texture) {
		_portrait.LoadPortrait(key, texture);
	}

	public void Update(GameTime gameTime) {
		if (!IsActive) {
			_rootPanel.Visible = false;
			return;
		}

		_rootPanel.Visible = true;

		DialogNode currentNode = _dialogManager.GetCurrentNode();
		if (currentNode == null) {
			return;
		}

		// Update content if node changed
		if (_currentNodeId != currentNode.Id) {
			_currentNodeId = currentNode.Id;

			// Get localized text
			string npcText = _dialogManager.Localization.GetString(currentNode.TextKey);
			NPCDefinition npc = _dialogManager.GetCurrentNPC();
			string npcName = npc != null ? _dialogManager.Localization.GetString(npc.NameKey) : "???";

			// Update dialog text
			_dialogText.SetText(npcName, npcText);

			// Update portrait
			string portraitKey = currentNode.PortraitKey ?? npc?.DefaultPortrait ?? "default";
			_portrait.SetPortrait(portraitKey);

			// Update responses
			UpdateResponses();
		}
		bool textComplete = _dialogText.IsTextComplete;

		_responsePanel.Visible = textComplete;
		_responsePanel.Enabled = textComplete;

		// Update UI hierarchy
		_dialogText.Update(gameTime);
		_rootPanel.Update(gameTime);

		MouseState mouseState = Mouse.GetState();
		MouseState previousMouse = _previousMouseState;

		// Scale mouse position (same as GameMenu does)
		MouseState scaledMouse = ScaleMouseState(mouseState);
		MouseState scaledPrevMouse = ScaleMouseState(previousMouse);

		// Handle mouse input for buttons
		_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

		// Update previous mouse state
		_previousMouseState = mouseState;

		// Handle input
		HandleInput();
	}

	private void UpdateResponses() {
		// Clear old response buttons
		foreach (UIButton button in _responseButtons) {
			_responsePanel.RemoveChild(button);
		}
		_responseButtons.Clear();

		// Get available responses
		List<DialogResponse> responses = _dialogManager.GetAvailableResponses();
		if (responses.Count == 0) {
			return;
		}

		// Create new response buttons
		const int RESPONSE_HEIGHT = 18;

		for (int i = 0; i < responses.Count; i++) {
			int responseIndex = i;  // Capture for lambda
			string responseText = _dialogManager.Localization.GetString(responses[i].TextKey);

			UIButton button = new UIButton(responseText) {
				Width = _responsePanel.Width,
				Height = RESPONSE_HEIGHT,
				BackgroundColor = Color.Transparent,
				HoverColor = new Color(155, 155, 155, 50),
				TextColor = Color.LightGray,
				HoverTextColor = Color.Yellow,
				BorderColor = Color.Transparent,
				Alignment = UIButton.TextAlignment.Left,
				TextPadding = 10,
				OnClick = () => ChooseResponse(responseIndex)
			};

			_responsePanel.AddChild(button);
			_responseButtons.Add(button);
		}

		_selectedResponseIndex = 0;

		UpdateButtonHighlights();
	}

	private void HandleInput() {
		KeyboardState keyState = Keyboard.GetState();

		// Skip typewriter if still typing
		if (!_dialogText.IsTextComplete) {
			// Click or press to complete text
			if (Mouse.GetState().LeftButton == ButtonState.Pressed ||
			   keyState.IsKeyDown(Keys.Space) ||
			   keyState.IsKeyDown(Keys.Enter)) {
				_dialogText.CompleteText();
			}
			_previousKeyState = keyState;
			return;
		}

		// Arrow key navigation
		if (_responseButtons.Count > 0) {
			// Down / S - move to next response
			if ((keyState.IsKeyDown(Keys.Down) && _previousKeyState.IsKeyUp(Keys.Down)) ||
			   (keyState.IsKeyDown(Keys.S) && _previousKeyState.IsKeyUp(Keys.S))) {
				_selectedResponseIndex = (_selectedResponseIndex + 1) % _responseButtons.Count;
				UpdateButtonHighlights();
			}

			// Up / W - move to previous response
			if ((keyState.IsKeyDown(Keys.Up) && _previousKeyState.IsKeyUp(Keys.Up)) ||
			   (keyState.IsKeyDown(Keys.W) && _previousKeyState.IsKeyUp(Keys.W))) {
				_selectedResponseIndex = (_selectedResponseIndex - 1 + _responseButtons.Count) % _responseButtons.Count;
				UpdateButtonHighlights();
			}

			// Enter or Space to select
			if ((keyState.IsKeyDown(Keys.Enter) && _previousKeyState.IsKeyUp(Keys.Enter)) ||
			   (keyState.IsKeyDown(Keys.Space) && _previousKeyState.IsKeyUp(Keys.Space))) {
				ChooseResponse(_selectedResponseIndex);
			}
		}

		_previousKeyState = keyState;
	}

	private void UpdateButtonHighlights() {
		for (int i = 0; i < _responseButtons.Count; i++) {
			UIButton button = _responseButtons[i];
			string baseText = _dialogManager.Localization.GetString(
				_dialogManager.GetAvailableResponses()[i].TextKey
			);

			if (i == _selectedResponseIndex) {
				button.Text = "> " + baseText;
				button.TextColor = Color.Yellow;
			} else {
				button.Text = "  " + baseText;
				button.TextColor = Color.LightGray;
			}
		}
	}

	private void ChooseResponse(int index) {
		_dialogManager.ChooseResponse(index);
		_selectedResponseIndex = 0;
		_dialogText.ResetTypewriter();
		OnResponseChosen?.Invoke();
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (!IsActive) {
			return;
		}

		// Draw entire UI hierarchy with one call
		_rootPanel.Draw(spriteBatch);
	}
}