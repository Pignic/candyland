using EldmeresTale.Audio;
using EldmeresTale.Core.Saves;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Scenes;
using EldmeresTale.Systems;
using EldmoresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core;

public class ApplicationContext : IDisposable {

	public SoundEffectPlayer SoundEffects { get; private set; }
	public LocalizationManager Localization { get; }
	public SceneManager Scenes { get; }
	public BitmapFont Font { get; }
	public DisplayManager Display { get; }
	public InputSystem Input { get; }
	public InputLegend InputLegend { get; }
	public SaveManager SaveManager { get; private set; }
	public GameEventBus EventBus { get; private set; }

	public Game game { get; }

	public event Action<int, int> ResolutionRequested;

	public event Action<bool> FullscreenToggleRequested;

	public GraphicsDevice graphicsDevice => game.GraphicsDevice;

	public AssetManager assetManager { get; private set; }
	public MusicPlayer MusicPlayer { get; private set; }

	public ApplicationContext(Game game) {
		this.game = game;
		EventBus = new GameEventBus();
		TileRegistry.Instance.LoadFromFile("Assets/Terrain/tiles.json");
		MusicPlayer = new MusicPlayer {
			Volume = GameSettings.Instance.MusicVolume
		};
		Font = new BitmapFont(graphicsDevice);
		Localization = new LocalizationManager();
		Display = new DisplayManager(640, 360);
		assetManager = new AssetManager(graphicsDevice, game.Content);

		Input = new InputSystem(graphicsDevice);
		Input.Initialize();
		SaveManager = new SaveManager();
		InputLegend = new InputLegend(Input, Font);

		Scenes = new SceneManager(this);

		Localization.loadLanguage("en", "Assets/UI/Localization/en.json");

		SoundEffects = new SoundEffectPlayer();
		SoundEffects.LoadLibrary("Assets/Audio/sound_effects.json");
		SoundEffects.MasterVolume = GameSettings.Instance.SfxVolume;

		Scenes.Replace(new MainMenuScene(this));
	}

	private Player CreatePlayer() {
		const int TILE_SIZE = 16;

		Texture2D playerTexture = assetManager.LoadTextureOrFallback(
			"Assets/Sprites/player.png",
			() => Graphics.CreateColoredTexture(graphicsDevice, TILE_SIZE, TILE_SIZE, Color.Yellow)
		);

		Vector2 tempPosition = Vector2.Zero;
		Player player;

		if (playerTexture != null && playerTexture.Width == 96) {
			// Animated sprite sheet
			player = new Player(
				playerTexture,
				tempPosition,
				frameCount: 3,
				frameWidth: 32,
				frameHeight: 32,
				frameTime: 0.1f,
				width: TILE_SIZE,
				height: TILE_SIZE
			);
		} else {
			// Static sprite
			player = new Player(
				playerTexture,
				tempPosition,
				width: TILE_SIZE,
				height: TILE_SIZE
			);
		}

		// Initialize attack effect
		player.InitializeAttackEffect(graphicsDevice);

		return player;
	}

	private GameServices CreateGameServices(Player player) {
		GameServices gameServices = new GameServices(
			player,
			Localization,
			assetManager,
			graphicsDevice
		);

		// Load rooms
		gameServices.LoadRooms();

		return gameServices;
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
		InputCommands inputCommands = Input.GetCommands();
		InputLegend.Update(inputCommands, gameTime);
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
		Player player = CreatePlayer();

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
