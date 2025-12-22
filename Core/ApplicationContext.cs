using Candyland.Core.UI;
using Candyland.Dialog;
using Candyland.Scenes;
using Candyland.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.Core;

public class ApplicationContext : IDisposable {

	public LocalizationManager Localization { get; }
	public SceneManager Scenes { get; }
	public BitmapFont Font { get; }
	public DisplayManager Display { get; }
	public InputSystem Input { get; }
	public InputLegend InputLegend { get; }

	public Game game { get; }

	public event Action<int, int> ResolutionRequested;

	public event Action<bool> FullscreenToggleRequested;

	public GraphicsDevice graphicsDevice => game.GraphicsDevice;

	public GameServices gameState { get; private set; }

	public AssetManager assetManager { get; private set; }

	public ApplicationContext(Game game) {
		this.game = game;
		Font = new BitmapFont(graphicsDevice);
		Localization = new LocalizationManager();
		Display = new DisplayManager(640, 360);
		assetManager = new AssetManager(graphicsDevice, game.Content);
		gameState = GameServices.Initialize(this);

		Input = new InputSystem(graphicsDevice);
		Input.Initialize();
		InputLegend = new InputLegend(Input, Font);

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
		Input.Update(gameTime);
		Scenes.Update(gameTime);
		var inputCommands = Input.GetCommands();
		InputLegend.Update(inputCommands, gameTime);
	}

	public void Dispose() {
		Scenes.Dispose();
		Input.Dispose();
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

	public void OpenMapEditor(Camera camera) {
		Scenes.Push(new MapEditorScene(this, camera));
	}

	public void CloseScene() {
		Scenes.Pop();
	}

	public void StartDialog(string dialogId) {
		Scenes.Push(new DialogScene(this, dialogId));
	}
}
