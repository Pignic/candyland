using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Scenes;

internal class MainMenuScene : Scene {
	const int BUTTON_WIDTH = 200;
	const int BUTTON_HEIGHT = 30;
	const int BUTTON_SPACING = 10;

	// UI Components
	private UIPanel _rootPanel;
	private UIButton _newGameButton;
	private UIButton _continueButton;
	private UIButton _optionsButton;
	private UIButton _creditsButton;
	private UIButton _quitButton;

	private List<UIButton> _buttons;
	private readonly NavigationController _navController;

	// Callbacks
	public Action OnNewGame { get; set; }
	public Action OnContinue { get; set; }
	public Action OnOptions { get; set; }
	public Action OnCredits { get; set; }
	public Action OnQuit { get; set; }

	// Check if save exists
	public bool HasSaveFile { get; set; } = false;


	public MainMenuScene(ApplicationContext appContext, bool exclusive = true) : base(appContext, exclusive) {
		_navController = new NavigationController {
			Mode = NavigationMode.Index,
			ItemCount = 5,  // 5 buttons
			WrapAround = true
		};
	}

	public override void Load() {
		base.Load();

		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;

		int menuX = (screenWidth - BUTTON_WIDTH) / 2;
		int startY = (screenHeight / 2) - 80;

		// Root panel
		_rootPanel = new UIPanel() {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = new Color(20, 20, 30) // Dark background
		};

		_buttons = [];

		// New Game button
		_newGameButton = new UIButton("NEW GAME") {
			X = menuX,
			Y = startY,
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnNewGame?.Invoke(),
			IsNavigable = true
		};
		_rootPanel.AddChild(_newGameButton);
		_buttons.Add(_newGameButton);

		// Continue button
		_continueButton = new UIButton("CONTINUE") {
			X = menuX,
			Y = startY + ((BUTTON_HEIGHT + BUTTON_SPACING) * 1),
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnContinue?.Invoke(),
			IsNavigable = true
		};
		_rootPanel.AddChild(_continueButton);
		_buttons.Add(_continueButton);

		// Options button
		_optionsButton = new UIButton("OPTIONS") {
			X = menuX,
			Y = startY + ((BUTTON_HEIGHT + BUTTON_SPACING) * 2),
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnOptions?.Invoke(),
			IsNavigable = true
		};
		_rootPanel.AddChild(_optionsButton);
		_buttons.Add(_optionsButton);

		// Credits button
		_creditsButton = new UIButton("CREDITS") {
			X = menuX,
			Y = startY + ((BUTTON_HEIGHT + BUTTON_SPACING) * 3),
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnCredits?.Invoke(),
			IsNavigable = true
		};
		_rootPanel.AddChild(_creditsButton);
		_buttons.Add(_creditsButton);

		// Quit button
		_quitButton = new UIButton("QUIT") {
			X = menuX,
			Y = startY + ((BUTTON_HEIGHT + BUTTON_SPACING) * 4),
			Width = BUTTON_WIDTH,
			Height = BUTTON_HEIGHT,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnQuit?.Invoke(),
			IsNavigable = true
		};
		_rootPanel.AddChild(_quitButton);
		_buttons.Add(_quitButton);

		OnNewGame = StartNewGame;
		OnContinue = ContinueGame;
		OnOptions = OpenOptions;
		OnCredits = OpenCredits;
		OnQuit = Quit;

		HasSaveFile = appContext.SaveManager.SaveExists("test_save");
		_continueButton.Enabled = HasSaveFile;
		_continueButton.TextColor = HasSaveFile ? Color.White : Color.Gray;
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);

		InputCommands input = appContext.Input.GetCommands();
		_navController.Update(input);
		for (int i = 0; i < _buttons.Count; i++) {
			if (_navController.IsSelected(i)) {
				// Fake hover state for keyboard selection
				_buttons[i].ForceHoverState(true);
			} else {
				_buttons[i].ForceHoverState(false);
			}
		}
		MouseState mouseState = Mouse.GetState();
		Point mouseScaled = appContext.Display.ScaleMouseState(mouseState).Position;
		for (int i = 0; i < _buttons.Count; i++) {
			if (_buttons[i].GlobalBounds.Contains(mouseScaled)) {
				_navController.SetSelectedIndex(i);
				break;
			}
		}
		if (input.InteractPressed) {
			int selected = _navController.SelectedIndex;
			if (selected >= 0 && selected < _buttons.Count) {
				UIButton button = _buttons[selected];
				if (button.Enabled) {
					button.Click();
				}
			}
		}

		// Update enabled state of Continue button
		_continueButton.Enabled = HasSaveFile;
		_continueButton.TextColor = HasSaveFile ? Color.White : Color.Gray;

		// Update UI
		_rootPanel.Update(gameTime);
	}


	public override void Draw(SpriteBatch spriteBatch) {
		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;
		BitmapFont font = appContext.Font;
		// Draw background
		_rootPanel.Draw(spriteBatch);

		// Draw title with fancy effect
		string title = "Eldmere's Tale";
		int titleWidth = font.MeasureString(title) * 3; // 3x scale for title
		int titleX = (_rootPanel.Width - titleWidth) / 2;
		int titleY = 40;

		// Title shadow
		font.DrawText(spriteBatch, title,
			new Vector2(titleX + 3, titleY + 3),
			Color.Black, null, null, 3f);

		// Title with rainbow effect (cycle colors)
		Color titleColor = Color.Lerp(Color.Gold, Color.Orange,
			((float)Math.Sin(DateTime.Now.Millisecond / 500.0) * 0.5f) + 0.5f);
		font.DrawText(spriteBatch, title,
			new Vector2(titleX, titleY),
			titleColor, null, null, 3f);

		// Version text
		font.DrawText(spriteBatch, "v0.1.0",
			new Vector2(10, _rootPanel.Height - 20),
			Color.Gray);

		// Credits hint
		font.DrawText(spriteBatch, "",
			new Vector2(_rootPanel.Width - 150, _rootPanel.Height - 20),
			Color.Gray);

		appContext.InputLegend.Draw(
			spriteBatch,
			screenWidth,
			screenHeight,
			(GameAction.Interact, "Select"),
			(GameAction.Cancel, "Quit")
		);

		base.Draw(spriteBatch);
	}

	private void ActivateButton(int index) {
		switch (index) {
			case 0: OnNewGame?.Invoke(); break;
			case 1: if (HasSaveFile) { OnContinue?.Invoke(); } break;
			case 2: OnOptions?.Invoke(); break;
			case 3: OnCredits?.Invoke(); break;
			case 4: OnQuit?.Invoke(); break;
		}
	}



	private bool CheckForSaveFile() {
		// TODO: Check if save file exists
		return false;
	}

	private void StartNewGame() {
		appContext.StartNewGame();
	}

	private void ContinueGame() {
		appContext.StartNewGame(loadSave: true, saveName: "test_save");
	}

	private void ResetGame() {

	}

	private void OpenOptions() {

	}

	private void OpenCredits() {

	}

	private void Quit() {
		appContext.Game.Exit();
	}
}
