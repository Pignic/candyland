using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using EldmeresTale.Dialog;

namespace EldmeresTale.Core.UI;

/// <summary>
/// Refactored dialog UI using UIPanel/UIElement hierarchy
/// Much cleaner and consistent with the rest of the UI system
/// </summary>
public class UIDialog {
	private DialogManager _dialogManager;
	private BitmapFont _font;
	private GraphicsDevice _graphicsDevice;

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

	public bool isActive => _dialogManager.isDialogActive;

	public void SetScale(int newScale) {
		_scale = newScale;
	}

	public UIDialog(DialogManager dialogManager, BitmapFont font, GraphicsDevice graphicsDevice,
					int screenWidth, int screenHeight, int scale) {
		_dialogManager = dialogManager;
		_font = font;
		_graphicsDevice = graphicsDevice;
		_scale = scale;

		buildUI(screenWidth, screenHeight);

		_previousMouseState = Mouse.GetState();
	}
	private MouseState scaleMouseState(MouseState original) {
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

	private void buildUI(int screenWidth, int screenHeight) {
		const int DIALOG_HEIGHT = 120;
		const int MARGIN = 10;
		const int PORTRAIT_SIZE = 60;
		const int PORTRAIT_MARGIN = 5;

		// === ROOT PANEL ===
		_rootPanel = new UIPanel(_graphicsDevice) {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = Color.Transparent,
			Visible = false
		};

		// === DIALOG BOX PANEL (bottom of screen) ===
		_dialogBoxPanel = new UIPanel(_graphicsDevice) {
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
		_portrait = new UIPortrait(_graphicsDevice) {
			X = PORTRAIT_MARGIN,
			Y = PORTRAIT_MARGIN,
			Width = PORTRAIT_SIZE,
			Height = PORTRAIT_SIZE
		};
		_dialogBoxPanel.AddChild(_portrait);

		// === DIALOG TEXT (with typewriter and NPC name) ===
		int textX = PORTRAIT_SIZE + PORTRAIT_MARGIN * 2;
		int textWidth = _dialogBoxPanel.Width - textX - PORTRAIT_MARGIN;

		_dialogText = new UIDialogText(_font) {
			X = textX,
			Y = PORTRAIT_MARGIN,
			Width = textWidth,
			Height = PORTRAIT_SIZE
		};
		_dialogBoxPanel.AddChild(_dialogText);

		// === RESPONSE PANEL (below dialog text) ===
		_responsePanel = new UIPanel(_graphicsDevice) {
			X = textX,
			Y = PORTRAIT_SIZE + PORTRAIT_MARGIN + 5,
			Width = textWidth,
			Height = DIALOG_HEIGHT - PORTRAIT_SIZE - PORTRAIT_MARGIN * 2 - 5,
			BackgroundColor = Color.Transparent,
			BorderColor = Color.Transparent,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 2
		};
		_dialogBoxPanel.AddChild(_responsePanel);

		_responseButtons = new List<UIButton>();
		_previousKeyState = Keyboard.GetState();
	}

	public void loadPortrait(string key, Texture2D texture) {
		_portrait.loadPortrait(key, texture);
	}

	public void update(GameTime gameTime) {
		if(!isActive) {
			_rootPanel.Visible = false;
			return;
		}

		_rootPanel.Visible = true;

		var currentNode = _dialogManager.getCurrentNode();
		if(currentNode == null) return;

		// Update content if node changed
		if(_currentNodeId != currentNode.id) {
			_currentNodeId = currentNode.id;

			// Get localized text
			string npcText = _dialogManager.Localization.getString(currentNode.textKey);
			var npc = _dialogManager.getCurrentNPC();
			string npcName = npc != null ? _dialogManager.Localization.getString(npc.nameKey) : "???";

			// Update dialog text
			_dialogText.setText(npcName, npcText);

			// Update portrait
			string portraitKey = currentNode.portraitKey ?? (npc?.defaultPortrait ?? "default");
			_portrait.setPortrait(portraitKey);

			// Update responses
			updateResponses();
		}
		bool textComplete = _dialogText.isTextComplete;

		_responsePanel.Visible = textComplete;
		_responsePanel.Enabled = textComplete;

		// Update UI hierarchy
		_dialogText.update(gameTime);
		_rootPanel.Update(gameTime);

		var mouseState = Mouse.GetState();
		var previousMouse = _previousMouseState;

		// Scale mouse position (same as GameMenu does)
		MouseState scaledMouse = scaleMouseState(mouseState);
		MouseState scaledPrevMouse = scaleMouseState(previousMouse);

		// Handle mouse input for buttons
		_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

		// Update previous mouse state
		_previousMouseState = mouseState;

		// Handle input
		handleInput();
	}

	private void updateResponses() {
		// Clear old response buttons
		foreach(var button in _responseButtons) {
			_responsePanel.RemoveChild(button);
		}
		_responseButtons.Clear();

		// Get available responses
		var responses = _dialogManager.getAvailableResponses();
		if(responses.Count == 0) return;

		// Create new response buttons
		const int RESPONSE_HEIGHT = 18;

		for(int i = 0; i < responses.Count; i++) {
			int responseIndex = i;  // Capture for lambda
			string responseText = _dialogManager.Localization.getString(responses[i].textKey);

			var button = new UIButton(_graphicsDevice, _font, responseText) {
				Width = _responsePanel.Width,
				Height = RESPONSE_HEIGHT,
				BackgroundColor = Color.Transparent,
				HoverColor = new Color(155, 155, 155, 50),
				TextColor = Color.LightGray,
				HoverTextColor = Color.Yellow,
				BorderColor = Color.Transparent,
				Alignment = UIButton.TextAlignment.Left,
				TextPadding = 10,
				OnClick = () => chooseResponse(responseIndex)
			};

			_responsePanel.AddChild(button);
			_responseButtons.Add(button);
		}

		_selectedResponseIndex = 0;

		updateButtonHighlights();
	}

	private void handleInput() {
		var keyState = Keyboard.GetState();

		// Skip typewriter if still typing
		if(!_dialogText.isTextComplete) {
			// Click or press to complete text
			if(Mouse.GetState().LeftButton == ButtonState.Pressed ||
			   keyState.IsKeyDown(Keys.Space) ||
			   keyState.IsKeyDown(Keys.Enter)) {
				_dialogText.completeText();
			}
			_previousKeyState = keyState;
			return;
		}

		// Arrow key navigation
		if(_responseButtons.Count > 0) {
			// Down / S - move to next response
			if((keyState.IsKeyDown(Keys.Down) && _previousKeyState.IsKeyUp(Keys.Down)) ||
			   (keyState.IsKeyDown(Keys.S) && _previousKeyState.IsKeyUp(Keys.S))) {
				_selectedResponseIndex = (_selectedResponseIndex + 1) % _responseButtons.Count;
				updateButtonHighlights();
			}

			// Up / W - move to previous response
			if((keyState.IsKeyDown(Keys.Up) && _previousKeyState.IsKeyUp(Keys.Up)) ||
			   (keyState.IsKeyDown(Keys.W) && _previousKeyState.IsKeyUp(Keys.W))) {
				_selectedResponseIndex = (_selectedResponseIndex - 1 + _responseButtons.Count) % _responseButtons.Count;
				updateButtonHighlights();
			}

			// Enter or Space to select
			if((keyState.IsKeyDown(Keys.Enter) && _previousKeyState.IsKeyUp(Keys.Enter)) ||
			   (keyState.IsKeyDown(Keys.Space) && _previousKeyState.IsKeyUp(Keys.Space))) {
				chooseResponse(_selectedResponseIndex);
			}
		}

		_previousKeyState = keyState;
	}

	private void updateButtonHighlights() {
		for(int i = 0; i < _responseButtons.Count; i++) {
			var button = _responseButtons[i];
			var baseText = _dialogManager.Localization.getString(
				_dialogManager.getAvailableResponses()[i].textKey
			);

			if(i == _selectedResponseIndex) {
				button.Text = "> " + baseText;
				button.TextColor = Color.Yellow;
			} else {
				button.Text = "  " + baseText;
				button.TextColor = Color.LightGray;
			}
		}
	}

	private void chooseResponse(int index) {
		_dialogManager.chooseResponse(index);
		_selectedResponseIndex = 0;
		_dialogText.resetTypewriter();
	}

	public void draw(SpriteBatch spriteBatch) {
		if(!isActive) return;

		// Draw entire UI hierarchy with one call
		_rootPanel.Draw(spriteBatch);
	}
}