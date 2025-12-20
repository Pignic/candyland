using Candyland.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Candyland {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private ApplicationContext _app;
		private RenderTarget2D _renderTarget;

		// === RESOLUTION CONSTANTS ===
		private const int NATIVE_WIDTH = 640;
		private const int NATIVE_HEIGHT = 360;
		private static int SCALE = 2;  // 3x for 1920x1080, 2x for 1280x720
		private int DISPLAY_WIDTH = NATIVE_WIDTH * SCALE;
		private int DISPLAY_HEIGHT = NATIVE_HEIGHT * SCALE;

		public Game1() {
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			// Set window size
			_graphics.PreferredBackBufferWidth = DISPLAY_WIDTH;
			_graphics.PreferredBackBufferHeight = DISPLAY_HEIGHT;
			_graphics.SynchronizeWithVerticalRetrace = true;
			Window.AllowUserResizing = true;
		}

		protected override void Initialize() {
			_spriteBatch = new SpriteBatch(GraphicsDevice);
			_app = new ApplicationContext(this);
			_app.ResolutionRequested += OnResolutionRequested;
			_app.FullscreenToggleRequested += OnFullscreenToggleRequested;
			CreateRenderTarget();
			base.Initialize();
		}

		private void CreateRenderTarget() {
			_renderTarget?.Dispose();
			_renderTarget = new RenderTarget2D(
				GraphicsDevice,
				_app.Display.VirtualWidth,
				_app.Display.VirtualHeight,
				false,
				SurfaceFormat.Color,
				DepthFormat.None,
				0,
				RenderTargetUsage.DiscardContents
			);
		}

		private void OnResolutionRequested(int width, int height) {
			_graphics.PreferredBackBufferWidth = width;
			_graphics.PreferredBackBufferHeight = height;
			_graphics.ApplyChanges();
		}

		private void OnFullscreenToggleRequested(bool isFullscreen) {
			_graphics.ToggleFullScreen();
			_graphics.ApplyChanges();
		}

		protected override void LoadContent() {

		}

		protected override void Update(GameTime gameTime) {
			_app.Display.Update(GraphicsDevice);
			_app.Update(gameTime);
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.SetRenderTarget(_renderTarget);
			GraphicsDevice.Clear(Color.Black);

			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);

			_app.Draw(_spriteBatch);

			_spriteBatch.End();

			// Composite to screen
			GraphicsDevice.SetRenderTarget(null);
			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
			_spriteBatch.Draw(_renderTarget, _app.Display.Letterbox, Color.White);
			_spriteBatch.End();

			base.Draw(gameTime);
		}


		protected override void Dispose(bool disposing) {
			if(disposing) {
				_renderTarget?.Dispose();
				_spriteBatch?.Dispose();
				_app?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
