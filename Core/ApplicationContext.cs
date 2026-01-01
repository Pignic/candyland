using EldmeresTale.Audio;
using EldmeresTale.Core.Saves;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.Scenes;
using EldmeresTale.Systems;
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

	public Game game { get; }

	public event Action<int, int> ResolutionRequested;

	public event Action<bool> FullscreenToggleRequested;

	public GraphicsDevice graphicsDevice => game.GraphicsDevice;

	public GameServices gameState { get; private set; }

	public AssetManager assetManager { get; private set; }
	public MusicPlayer MusicPlayer { get; private set; }

	public ApplicationContext(Game game) {
		this.game = game;

		MusicPlayer = new MusicPlayer();
		MusicPlayer.Volume = GameSettings.Instance.MusicVolume;
		Font = new BitmapFont(graphicsDevice);
		Localization = new LocalizationManager();
		Display = new DisplayManager(640, 360);
		assetManager = new AssetManager(graphicsDevice, game.Content);
		gameState = GameServices.Initialize(this);

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
		var inputCommands = Input.GetCommands();
		InputLegend.Update(inputCommands, gameTime);
	}

	public void Dispose() {
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
		Scenes.Replace(new GameScene(this, loadSave, saveName));
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
