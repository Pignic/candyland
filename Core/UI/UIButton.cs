using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Candyland.Core.UI {
	/// <summary>
	/// Clickable button with text
	/// </summary>
	public class UIButton : UIElement {
		private BitmapFont _font;
		private Texture2D _pixelTexture;

		public string Text { get; set; }
		public Action OnClick { get; set; }

		// Styling
		public Color BackgroundColor { get; set; } = new Color(60, 60, 60);
		public Color HoverColor { get; set; } = new Color(80, 80, 80);
		public Color PressedColor { get; set; } = new Color(40, 40, 40);
		public Color TextColor { get; set; } = Color.White;
		public Color BorderColor { get; set; } = Color.White;
		public int BorderWidth { get; set; } = 1;

		// State
		private bool _isHovered = false;
		private bool _isPressed = false;

		public UIButton(GraphicsDevice graphicsDevice, BitmapFont font, string text) {
			_font = font;
			Text = text;

			_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			_pixelTexture.SetData(new[] { Color.White });

			// Default size based on text
			int textWidth = font.measureString(text);
			int textHeight = font.getHeight();
			Width = textWidth + 20; // Padding
			Height = textHeight + 10;
		}

		protected override void OnDraw(SpriteBatch spriteBatch) {
			var globalBounds = GlobalBounds;

			// Determine background color based on state
			Color bgColor = BackgroundColor;
			if(!Enabled)
				bgColor = BackgroundColor * 0.5f;
			else if(_isPressed)
				bgColor = PressedColor;
			else if(_isHovered)
				bgColor = HoverColor;

			// Background
			spriteBatch.Draw(_pixelTexture, globalBounds, bgColor);

			// Border
			if(BorderWidth > 0) {
				DrawBorder(spriteBatch, globalBounds, BorderColor, BorderWidth);
			}

			// Text (centered)
			if(!string.IsNullOrEmpty(Text)) {
				int textWidth = _font.measureString(Text);
				int textHeight = _font.getHeight();
				int textX = globalBounds.X + (globalBounds.Width - textWidth) / 2;
				int textY = globalBounds.Y + (globalBounds.Height - textHeight) / 2;

				_font.drawText(spriteBatch, Text, new Vector2(textX, textY), TextColor);
			}
		}

		protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
			if(!Enabled) return false;

			Point mousePos = mouse.Position;
			_isHovered = GlobalBounds.Contains(mousePos);

			// Check for click
			if(_isHovered) {
				// Pressed
				if(mouse.LeftButton == ButtonState.Pressed) {
					_isPressed = true;
				}

				// Released (click complete)
				if(mouse.LeftButton == ButtonState.Released &&
					previousMouse.LeftButton == ButtonState.Pressed &&
					_isPressed) {
					_isPressed = false;
					OnClick?.Invoke();
					return true;
				}
			} else {
				_isPressed = false;
			}

			return _isHovered; // Consume input if hovered
		}

		private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int width) {
			// Top
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
			// Bottom
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
			// Left
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
			// Right
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
		}
	}
}