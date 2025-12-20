using Candyland.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Candyland.Scenes;

internal class GameMenuScene : Scene {

	private GameMenu _gameMenu;
	private KeyboardState _previousKeyState;

	public GameMenuScene(ApplicationContext appContext) : base(appContext, exclusive: true) {

		_gameMenu = new GameMenu(
			appContext.graphicsDevice,
			appContext.Font,
			appContext.gameState.Player,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight,
			appContext.Display.Scale,
			appContext.gameState.QuestManager
		);
		_gameMenu.IsOpen = true;
		_previousKeyState = Keyboard.GetState();

		_gameMenu.OnScaleChanged += OnScaleChanged;
		_gameMenu.OnFullscreenChanged += OnFullscreenChanged;
	}
	private void OnScaleChanged(int newScale) {
		System.Diagnostics.Debug.WriteLine($"[GAMEMENUSCENE] Scale changed to: {newScale}");

		// Request resolution change through ApplicationContext
		int newWidth = appContext.Display.VirtualWidth * newScale;
		int newHeight = appContext.Display.VirtualHeight * newScale;

		appContext.RequestResolutionChange(newWidth, newHeight);

		// Update GameMenu's internal scale
		_gameMenu.SetScale(newScale);
	}

	private void OnFullscreenChanged(bool isFullscreen) {
		System.Diagnostics.Debug.WriteLine($"[GAMEMENUSCENE] Fullscreen changed to: {isFullscreen}");

		appContext.RequestFullscreenChange(isFullscreen);
	}

	public override void Update(GameTime time) {
		KeyboardState keyState = Keyboard.GetState();

		// Close menu with Tab or Escape
		if((keyState.IsKeyDown(Keys.Tab) && _previousKeyState.IsKeyUp(Keys.Tab)) ||
		   (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))) {
			appContext.CloseScene();
		}

		_gameMenu.Update(time);
		_previousKeyState = keyState;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous scene's batch
		spriteBatch.End();

		// Begin fresh for menu
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		_gameMenu.Draw(spriteBatch);

		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);
	}
}
