using EldmeresTale.Audio;
using EldmeresTale.Core.Saves;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Scenes;
using EldmeresTale.Systems;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core;

public class ApplicationContext : IDisposable {

	public SoundEffectPlayer SoundEffects { get; }
	public LocalizationManager Localization { get; }
	public SceneManager Scenes { get; }
	public BitmapFont Font { get; }
	public DisplayManager Display { get; }
	public InputSystem Input { get; }
	public InputLegend InputLegend { get; }
	public SaveManager SaveManager { get; }
	public GameEventBus EventBus { get; }

	public Game Game { get; }

	public event Action<int, int> ResolutionRequested;

	public event Action<bool> FullscreenToggleRequested;

	public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

	public AssetManager AssetManager { get; }

	public MusicPlayer MusicPlayer { get; }

	public ApplicationContext(Game game) {
		Game = game;
		EventBus = new GameEventBus();
		TileRegistry.Instance.LoadFromFile("Assets/Terrain/tiles.json");
		MusicPlayer = new MusicPlayer {
			Volume = GameSettings.Instance.MusicVolume
		};
		Font = new BitmapFont(GraphicsDevice);
		Localization = new LocalizationManager();
		Display = new DisplayManager(640, 360);
		AssetManager = new AssetManager(GraphicsDevice, game.Content);

		Input = new InputSystem(GraphicsDevice);
		Input.Initialize();
		SaveManager = new SaveManager();
		InputLegend = new InputLegend(Input, Font);

		Scenes = new SceneManager();

		Localization.LoadLanguage("en", "Assets/UI/Localization/en.json");

		SoundEffects = new SoundEffectPlayer();
		SoundEffects.LoadLibrary("Assets/Audio/sound_effects.json");
		SoundEffects.MasterVolume = GameSettings.Instance.SfxVolume;

		Scenes.Replace(new MainMenuScene(this));
	}

	private GameServices CreateGameServices(Player player) {
		return new GameServices(
			player,
			Localization,
			AssetManager,
			GraphicsDevice
		);
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
		MusicPlayer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
		SoundEffects.Update();
		InputLegend.Update(Input.GetCommands(), gameTime);
	}

	public void Dispose() {
		EventBus?.Dispose();
		Scenes.Dispose();
		Input.Dispose();
		MusicPlayer?.Dispose();
		SoundEffects?.Dispose();
	}

	public void Draw(SpriteBatch spriteBatch) {
		Scenes.Draw(spriteBatch);
	}

	// Navigation functions
	public void StartNewGame(bool loadSave = false, string saveName = "test_save") {
		// Create player
		Player player = new Player(AssetManager.DefaultTexture);

		// Create game services
		GameServices gameServices = CreateGameServices(player);

		// Create game scene with services
		GameScene gameScene = new GameScene(this, gameServices, loadSave, saveName);

		Scenes.Replace(gameScene);
	}

	public void OpenGameMenu(GameServices gameServices) {
		Scenes.Push(new GameMenuScene(this, gameServices));
	}

	public void OpenMapEditor(Camera camera, GameServices gameServices) {
		Scenes.Push(new MapEditorScene(this, gameServices, camera));
	}

	public void CloseScene() {
		Scenes.Pop();
	}

	public void MainMenu() {
		Scenes.Replace(new MainMenuScene(this));
	}

	public void StartDialog(string dialogId, GameServices gameServices) {
		Scenes.Push(new DialogScene(this, gameServices, dialogId, Scenes.GetCamera()));
	}

	public void GameOver(RenderTarget2D target) {
		Scenes.Push(new DeathScreenOverlay(this, target));
	}
}
