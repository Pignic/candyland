using Candyland.Core.UI;
using Candyland.Dialog;
using Candyland.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.Core;

public class ApplicationContext : IDisposable {

	public LocalizationManager Localization { get; }
	public SceneManager Scenes { get; }
	public BitmapFont Font { get; }
	public int Scale { get; }
	public DisplayManager Display { get; }

	public Game game { get; }

	public event Action<int, int> ResolutionRequested;

	public event Action<bool> FullscreenToggleRequested;

	public GraphicsDevice graphicsDevice => game.GraphicsDevice;

	public GameServices gameState { get; private set; }

	public AssetManager assetManager { get; private set; }

	public ApplicationContext(Game game) {
		this.game = game;
		Font = new BitmapFont(game.GraphicsDevice);
		Localization = new LocalizationManager();
		Display = new DisplayManager(640, 360);
		assetManager = new AssetManager(game.GraphicsDevice);
		gameState = GameServices.Initialize(this);
		Scenes = new SceneManager(this);

		Localization.loadLanguage("en", "Assets/UI/Localization/en.json");
		Scenes.Replace(new MainMenuScene(this));
	}

	public void RequestResolutionChange(int width, int height) {
		ResolutionRequested?.Invoke(width, height);
	}

	public void RequestFullscreenChange(bool isFullscreen) {
		FullscreenToggleRequested?.Invoke(isFullscreen);
	}

	public void Update(GameTime gameTime) {
		Scenes.Update(gameTime);
	}

	public void Dispose() {
		Scenes.Dispose();
	}

	public void Draw(SpriteBatch spriteBatch) {
		Scenes.Draw(spriteBatch);
	}

	// Navigation functions
	public void StartNewGame() {
		Scenes.Replace(new GameScene(this));
	}

	public void OpenGameMenu() {
		Scenes.Push(new GameMenuScene(this));
	}

	public void CloseScene() {
		Scenes.Pop();
	}

	public void StartDialog(string dialogId) {
		Scenes.Push(new DialogScene(this, dialogId));
	}
}
