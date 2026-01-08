using EldmeresTale.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Core;

public class CreditsScreen {
	private readonly BitmapFont _font;
	private readonly GraphicsDevice _graphicsDevice;
	private int _scale;
	private KeyboardState _previousKeyState;
	private MouseState _previousMouseState;

	public System.Action OnBack;

	private UIPanel _rootPanel;
	private UIButton _backButton;

	public CreditsScreen(GraphicsDevice graphicsDevice, BitmapFont font,
						 int screenWidth, int screenHeight, int scale) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_scale = scale;

		BuildUI(screenWidth, screenHeight);
	}

	private void BuildUI(int screenWidth, int screenHeight) {
		_rootPanel = new UIPanel(_graphicsDevice) {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = new Color(20, 20, 30)
		};

		// Back button
		_backButton = new UIButton(_graphicsDevice, _font, "BACK") {
			X = (screenWidth - 100) / 2,
			Y = screenHeight - 60,
			Width = 100,
			Height = 30,
			BackgroundColor = new Color(60, 60, 80),
			HoverColor = new Color(100, 100, 120),
			TextColor = Color.White,
			HoverTextColor = Color.Yellow,
			OnClick = () => OnBack?.Invoke()
		};
		_rootPanel.AddChild(_backButton);
	}

	public void Update(GameTime gameTime) {
		KeyboardState keyState = Keyboard.GetState();
		MouseState mouseState = Mouse.GetState();

		// ESC or Enter to go back
		if ((keyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape)) ||
		   (keyState.IsKeyDown(Keys.Enter) && !_previousKeyState.IsKeyDown(Keys.Enter))) {
			OnBack?.Invoke();
		}

		_rootPanel.Update(gameTime);

		MouseState scaledMouse = ScaleMouseState(mouseState);
		MouseState scaledPrevMouse = ScaleMouseState(_previousMouseState);
		_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

		_previousKeyState = keyState;
		_previousMouseState = mouseState;
	}

	public void Draw(SpriteBatch spriteBatch) {
		_rootPanel.Draw(spriteBatch);

		int centerX = _rootPanel.Width / 2;
		int y = 100;

		// Title
		const string title = "CREDITS";
		int titleWidth = _font.MeasureString(title) * 2;
		_font.DrawText(spriteBatch, title,
			new Vector2(centerX - (titleWidth / 2), y),
			Color.Yellow, null, null, 2f);

		y += 60;

		// Credits content
		DrawCenteredText(spriteBatch, "Game Design & Programming", y, Color.LightGray);
		y += 20;
		DrawCenteredText(spriteBatch, "Pignic", y, Color.White);

		// Instructions
		_font.DrawText(spriteBatch, "Press ESC or ENTER to return",
			new Vector2(centerX - 120, _rootPanel.Height - 100),
			Color.Gray);
	}

	private void DrawCenteredText(SpriteBatch spriteBatch, string text, int y, Color color) {
		int textWidth = _font.MeasureString(text);
		_font.DrawText(spriteBatch, text,
			new Vector2((_rootPanel.Width - textWidth) / 2, y),
			color);
	}

	private MouseState ScaleMouseState(MouseState original) {
		Point scaledPosition = new Point(
			original.Position.X / _scale,
			original.Position.Y / _scale
		);

		return new MouseState(
			scaledPosition.X,
			scaledPosition.Y,
			original.ScrollWheelValue,
			original.LeftButton,
			original.MiddleButton,
			original.RightButton,
			original.XButton1,
			original.XButton2
		);
	}

	public void SetScale(int newScale) {
		_scale = newScale;
	}
}