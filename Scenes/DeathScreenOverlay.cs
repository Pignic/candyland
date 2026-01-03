using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

	// UI
	private UIPanel _rootPanel;
	private UILabel _youDiedLabel;
	private UIButton _continueButton;
	private UIButton _quitButton;

	// Render target for game scene
	private RenderTarget2D _gameSceneRenderTarget;

	public event Action OnContinue;
	public event Action OnQuit;
	private MouseState _previousMouseState;

	public DeathScreenOverlay(ApplicationContext appContext, RenderTarget2D gameSceneTarget)
		: base(appContext, false) {

		_gameSceneRenderTarget = gameSceneTarget;

		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;
		int centerX = screenWidth / 2;
		int centerY = screenHeight / 2;

		// Create root panel (fills screen, transparent)
		_rootPanel = new UIPanel(appContext.graphicsDevice) {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = Color.Transparent,
			BorderColor = Color.Transparent,
			Layout = UIPanel.LayoutMode.None,
			Visible = false  // Hidden during grayscale fade
		};

		// "YOU DIED" label
		_youDiedLabel = new UILabel(appContext.Font, "YOU DIED") {
			X = centerX - 50,  // Centered (adjust based on text width)
			Y = centerY - 80,
			TextColor = Color.Red * 0f  // Start invisible, will fade in
		};
		_youDiedLabel.UpdateSize();
		_rootPanel.AddChild(_youDiedLabel);

		// Continue button
		_continueButton = new UIButton(appContext.graphicsDevice, appContext.Font, "Continue") {
			X = centerX - 60,
			Y = centerY + 20,
			Width = 120,
			Height = 30,
			IsNavigable = true
		};
		_continueButton.OnClick += () => OnContinue?.Invoke();
		_rootPanel.AddChild(_continueButton);

		// Quit button
		_quitButton = new UIButton(appContext.graphicsDevice, appContext.Font, "Quit") {
			X = centerX - 60,
			Y = centerY + 60,
			Width = 120,
			Height = 30,
			IsNavigable = true
		};
		_quitButton.OnClick += () => OnQuit?.Invoke();
		_rootPanel.AddChild(_quitButton);

		// Wire up events
		OnContinue += OnDeathContinue;
		OnQuit += OnDeathQuit;

		_previousMouseState = Mouse.GetState();
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		_timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

		// State transitions
		if(State == DeathState.FadingToGrayscale && _timer >= GRAYSCALE_FADE_DURATION) {
			State = DeathState.ShowingUI;
			_timer = 0f;  // Reset for black fade
			_rootPanel.Visible = true;  // Show UI
		}

		// Update UI when showing
		if(State == DeathState.ShowingUI) {
			// Fade in "YOU DIED" text
			float uiFadeIn = MathHelper.Clamp(_timer / 0.5f, 0f, 1f);  // Fade in over 0.5s
			_youDiedLabel.TextColor = Color.Red * uiFadeIn;

			// Update panel
			_rootPanel.Update(gameTime);

			// FIXED - Get scaled mouse state
			var mouseState = Mouse.GetState();
			var scaledMouse = appContext.Display.ScaleMouseState(mouseState);
			var scaledPrevMouse = appContext.Display.ScaleMouseState(_previousMouseState);

			// Handle mouse input with scaled positions
			_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

			// Store for next frame
			_previousMouseState = mouseState;
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if(_gameSceneRenderTarget == null) return;

		// Don't call base.Draw() - we're doing custom rendering

		// Draw based on state
		if(State == DeathState.FadingToGrayscale) {
			DrawGrayscaleFade(spriteBatch);
		} else if(State == DeathState.ShowingUI) {
			DrawUIWithBlackFade(spriteBatch);
		}
	}

	private void DrawGrayscaleFade(SpriteBatch spriteBatch) {
		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;

		// Draw game scene
		spriteBatch.Draw(_gameSceneRenderTarget,
			new Rectangle(0, 0, screenWidth, screenHeight),
			Color.White);

		// Grayscale effect: overlay with darkening
		float grayscaleAmount = MathHelper.Clamp(_timer / GRAYSCALE_FADE_DURATION, 0f, 1f);

		// Draw darkening overlay (simulates desaturation)
		Color overlayColor = Color.Black * (grayscaleAmount * 0.5f);
		spriteBatch.Draw(appContext.assetManager.DefaultTexture,
			new Rectangle(0, 0, screenWidth, screenHeight),
			overlayColor);
	}

	private void DrawUIWithBlackFade(SpriteBatch spriteBatch) {
		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;

		// Calculate fade progress
		float blackFadeProgress = MathHelper.Clamp(_timer / BLACK_FADE_DURATION, 0f, 1f);

		// Draw desaturated game scene
		spriteBatch.Draw(_gameSceneRenderTarget,
			new Rectangle(0, 0, screenWidth, screenHeight),
			Color.White);

		// Grayscale overlay (full)
		spriteBatch.Draw(appContext.assetManager.DefaultTexture,
			new Rectangle(0, 0, screenWidth, screenHeight),
			Color.Black * 0.5f);

		// Black fade overlay (increases over time)
		Color blackOverlay = Color.Black * (blackFadeProgress * 0.8f);  // Max 80% black
		spriteBatch.Draw(appContext.assetManager.DefaultTexture,
			new Rectangle(0, 0, screenWidth, screenHeight),
			blackOverlay);

		// Draw UI panel (includes all buttons and labels)
		_rootPanel.Draw(spriteBatch);
	}

	private void OnDeathContinue() {
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
		appContext.MainMenu();
	}

	public override void Dispose() {
		_rootPanel = null;
		base.Dispose();
	}
}