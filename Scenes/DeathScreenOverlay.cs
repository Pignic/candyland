using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Scenes;

/// <summary>
/// Overlay that handles death screen effects and UI
/// </summary>
public class DeathScreenOverlay : Scene {

	// Death sequence state
	public enum DeathState {
		FadingToGrayscale,  // 0-3 seconds: fade to B&W
		ShowingUI,          // 3+ seconds: show UI, fade to black
		Complete
	}

	public DeathState State { get; private set; } = DeathState.FadingToGrayscale;

	private float _timer = 0f;
	private const float GRAYSCALE_FADE_DURATION = 3.0f;
	private const float BLACK_FADE_DURATION = 10.0f;

	// UI elements
	private UILabel _youDiedLabel;
	private UIButton _continueButton;
	private UIButton _quitButton;

	public event Action OnContinue;
	public event Action OnQuit;

	public DeathScreenOverlay(ApplicationContext appContext): base(appContext, false) {

		// Create UI elements (initially hidden)
		int centerX = appContext.Display.VirtualWidth / 2;
		int centerY = appContext.Display.VirtualHeight / 2;

		_youDiedLabel = new UILabel(appContext.Font, "YOU DIED") {
			TextColor = Color.Red,
			X = centerX - 40,  // Approximate centering
			Y = centerY - 60
		};
		_youDiedLabel.UpdateSize();

		_continueButton = new UIButton(appContext.graphicsDevice, appContext.Font, "Continue");
		_continueButton.OnClick += () => OnContinue?.Invoke();
		OnContinue += OnDeathContinue;

		_quitButton = new UIButton(appContext.graphicsDevice, appContext.Font, "Quit");
		_quitButton.OnClick += () => OnQuit?.Invoke();
		OnQuit += OnDeathQuit;
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		_timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

		// State transitions
		if(State == DeathState.FadingToGrayscale && _timer >= GRAYSCALE_FADE_DURATION) {
			State = DeathState.ShowingUI;
			_timer = 0f;  // Reset for black fade
		}

		// Update UI when showing
		if(State == DeathState.ShowingUI) {
			_continueButton.Update(gameTime);
			_quitButton.Update(gameTime);
		}
	}

	public void Draw(SpriteBatch spriteBatch, RenderTarget2D gameRenderTarget, int screenWidth, int screenHeight) {
		base.Draw(spriteBatch);
		// Draw based on state
		if(State == DeathState.FadingToGrayscale) {
			DrawGrayscaleFade(spriteBatch, gameRenderTarget, screenWidth, screenHeight);
		} else if(State == DeathState.ShowingUI) {
			DrawUIWithBlackFade(spriteBatch, gameRenderTarget, screenWidth, screenHeight);
		}
	}

	private void DrawGrayscaleFade(SpriteBatch spriteBatch, RenderTarget2D gameRender, int screenWidth, int screenHeight) {
		// Draw game scene
		spriteBatch.Draw(gameRender, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);

		// Grayscale effect: overlay with desaturated version
		float grayscaleAmount = MathHelper.Clamp(_timer / GRAYSCALE_FADE_DURATION, 0f, 1f);

		// Simple overlay approach - draw darkening overlay
		Color overlayColor = Color.Black * (grayscaleAmount * 0.5f);
		spriteBatch.Draw(appContext.assetManager.DefaultTexture, new Rectangle(0, 0, screenWidth, screenHeight), overlayColor);
	}

	private void DrawUIWithBlackFade(SpriteBatch spriteBatch, RenderTarget2D gameRender, int screenWidth, int screenHeight) {
		// Calculate fade progress
		float blackFadeProgress = MathHelper.Clamp(_timer / BLACK_FADE_DURATION, 0f, 1f);

		// Draw desaturated game scene
		spriteBatch.Draw(gameRender, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);

		// Grayscale overlay (full)
		spriteBatch.Draw(appContext.assetManager.DefaultTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.5f);

		// Black fade overlay (increases over time)
		Color blackOverlay = Color.Black * (blackFadeProgress * 0.8f);  // Max 80% black
		spriteBatch.Draw(appContext.assetManager.DefaultTexture, new Rectangle(0, 0, screenWidth, screenHeight), blackOverlay);

		// Draw UI elements
		_youDiedLabel.Draw(spriteBatch);
		_continueButton.Draw(spriteBatch);
		_quitButton.Draw(spriteBatch);
	}

	private void OnDeathContinue() {
		// Reload from last save or restart
		System.Diagnostics.Debug.WriteLine("[DEATH] Continue pressed");

		// Option 1: Reload last save
		if(appContext.SaveManager.SaveExists("autosave")) {
			appContext.StartNewGame(true, "autosave");
		} else {
			// Option 2: Just restart
			appContext.StartNewGame(false);
		}
	}

	private void OnDeathQuit() {
		System.Diagnostics.Debug.WriteLine("[DEATH] Quit pressed");
		// Return to main menu
		appContext.CloseScene();  // Or navigate to main menu
	}

	public override void Dispose() {
		base.Dispose();
	}
}