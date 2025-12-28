using EldmeresTale.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EldmeresTale.Core;

public class MainMenu {
	private GraphicsDevice _graphicsDevice;
	private BitmapFont _font;
	private int _scale;

	// UI Components
	private UIPanel _rootPanel;
	private UIButton _newGameButton;
	private UIButton _continueButton;
	private UIButton _optionsButton;
	private UIButton _creditsButton;
	private UIButton _quitButton;

	// State
	private int _selectedIndex = 0;
	private KeyboardState _previousKeyState;
	private MouseState _previousMouseState;

	// Callbacks
	public System.Action OnNewGame;
	public System.Action OnContinue;
	public System.Action OnOptions;
	public System.Action OnCredits;
	public System.Action OnQuit;

	// Check if save exists
	public bool HasSaveFile { get; set; } = false;

	public MainMenu(GraphicsDevice graphicsDevice, BitmapFont font,
					int screenWidth, int screenHeight, int scale) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_scale = scale;

		BuildUI(screenWidth, screenHeight);
	}

	private void BuildUI(int screenWidth, int screenHeight) {
		const int BUTTON_WIDTH = 200;
		const int BUTTON_HEIGHT = 30;
		const int BUTTON_SPACING = 10;

		int menuX = (screenWidth - BUTTON_WIDTH) / 2;
		int startY = screenHeight / 2 - 100;

		// Root panel
		_rootPanel = new UIPanel(_graphicsDevice) {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = new Color(20, 20, 30) // Dark background
		};

		// Title (we'll draw this separately for fancy effects)

		// New Game button
		_newGameButton = new UIButton(_graphicsDevice, _font, "NEW GAME") {
			X = menuX,
			Y = startY,
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnNewGame?.Invoke()
		};
		_rootPanel.AddChild(_newGameButton);

		// Continue button
		_continueButton = new UIButton(_graphicsDevice, _font, "CONTINUE") {
			X = menuX,
			Y = startY + (BUTTON_HEIGHT + BUTTON_SPACING) * 1,
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnContinue?.Invoke()
		};
		_rootPanel.AddChild(_continueButton);

		// Options button
		_optionsButton = new UIButton(_graphicsDevice, _font, "OPTIONS") {
			X = menuX,
			Y = startY + (BUTTON_HEIGHT + BUTTON_SPACING) * 2,
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnOptions?.Invoke()
		};
		_rootPanel.AddChild(_optionsButton);

		// Credits button
		_creditsButton = new UIButton(_graphicsDevice, _font, "CREDITS") {
			X = menuX,
			Y = startY + (BUTTON_HEIGHT + BUTTON_SPACING) * 3,
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnCredits?.Invoke()
		};
		_rootPanel.AddChild(_creditsButton);

		// Quit button
		_quitButton = new UIButton(_graphicsDevice, _font, "QUIT") {
			X = menuX,
			Y = startY + (BUTTON_HEIGHT + BUTTON_SPACING) * 4,
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnQuit?.Invoke()
		};
		_rootPanel.AddChild(_quitButton);
	}

	public void Update(GameTime gameTime) {
		KeyboardState keyState = Keyboard.GetState();
		MouseState mouseState = Mouse.GetState();

		// Update enabled state of Continue button
		_continueButton.Enabled = HasSaveFile;
		_continueButton.TextColor = HasSaveFile ? Color.White : Color.Gray;

		// Keyboard navigation
		if(keyState.IsKeyDown(Keys.Down) && !_previousKeyState.IsKeyDown(Keys.Down)) {
			_selectedIndex = (_selectedIndex + 1) % 5;
		}
		if(keyState.IsKeyDown(Keys.Up) && !_previousKeyState.IsKeyDown(Keys.Up)) {
			_selectedIndex = (_selectedIndex - 1 + 5) % 5;
		}

		// Enter to select
		if(keyState.IsKeyDown(Keys.Enter) && !_previousKeyState.IsKeyDown(Keys.Enter)) {
			ActivateButton(_selectedIndex);
		}

		// Update UI
		_rootPanel.Update(gameTime);

		// Mouse input
		MouseState scaledMouse = ScaleMouseState(mouseState);
		MouseState scaledPrevMouse = ScaleMouseState(_previousMouseState);
		_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

		_previousKeyState = keyState;
		_previousMouseState = mouseState;
	}

	private void ActivateButton(int index) {
		switch(index) {
			case 0: OnNewGame?.Invoke(); break;
			case 1: if(HasSaveFile) OnContinue?.Invoke(); break;
			case 2: OnOptions?.Invoke(); break;
			case 3: OnCredits?.Invoke(); break;
			case 4: OnQuit?.Invoke(); break;
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		// Draw background
		_rootPanel.Draw(spriteBatch);

		// Draw title with fancy effect
		string title = "CANDYLAND";
		int titleWidth = _font.measureString(title) * 3; // 3x scale for title
		int titleX = (_rootPanel.Width - titleWidth) / 2;
		int titleY = 80;

		// Title shadow
		_font.drawText(spriteBatch, title,
			new Vector2(titleX + 3, titleY + 3),
			Color.Black, null, null, 3f);

		// Title with rainbow effect (cycle colors)
		Color titleColor = Color.Lerp(Color.Gold, Color.Orange,
			(float)Math.Sin(DateTime.Now.Millisecond / 500.0) * 0.5f + 0.5f);
		_font.drawText(spriteBatch, title,
			new Vector2(titleX, titleY),
			titleColor, null, null, 3f);

		// Version text
		_font.drawText(spriteBatch, "v0.1.0",
			new Vector2(10, _rootPanel.Height - 20),
			Color.Gray);

		// Credits hint
		_font.drawText(spriteBatch, "Made with MonoGame",
			new Vector2(_rootPanel.Width - 150, _rootPanel.Height - 20),
			Color.Gray);
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

	public void SetScale(int newScale) {
		_scale = newScale;
	}
}