using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI {
	/// <summary>
	/// Progress bar (health, XP, etc.) compatible with new UI system
	/// </summary>
	public class UIProgressBar : UIElement {
		private BitmapFont _font;
		private Texture2D _pixelTexture;

		public Func<string> GetText { get; set; }
		public Func<float> GetValue { get; set; } // Returns 0-1

		// Styling
		public Color ForegroundColor { get; set; } = Color.Red;
		public Color TextColor { get; set; } = Color.White;
		public int TextMargin { get; set; } = 2;

		public UIProgressBar(GraphicsDevice graphicsDevice, BitmapFont font,
						   Func<string> getText, Func<float> getValue) {
			_font = font;
			GetText = getText;
			GetValue = getValue;

			_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			_pixelTexture.SetData(new[] { Color.White });

			// Default size
			Height = font.getHeight(TextMargin);
		}

		protected override void OnDraw(SpriteBatch spriteBatch) {
			var globalBounds = GlobalBounds;

			// Border
			if(BorderWidth > 0) {
				Rectangle borderBounds = new Rectangle(
					globalBounds.X - BorderWidth,
					globalBounds.Y - BorderWidth,
					globalBounds.Width + BorderWidth * 2,
					globalBounds.Height + BorderWidth * 2
				);
				spriteBatch.Draw(_pixelTexture, borderBounds, BorderColor);
			}

			// Background (empty portion)
			spriteBatch.Draw(_pixelTexture, globalBounds, BackgroundColor);

			// Foreground (filled portion)
			float value = MathHelper.Clamp(GetValue(), 0f, 1f);
			int filledWidth = (int)(globalBounds.Width * value);

			Rectangle filledBounds = new Rectangle(
				globalBounds.X,
				globalBounds.Y,
				filledWidth,
				globalBounds.Height
			);
			spriteBatch.Draw(_pixelTexture, filledBounds, ForegroundColor);

			// Text (centered)
			string text = GetText();
			if(!string.IsNullOrEmpty(text)) {
				int textWidth = _font.measureString(text);
				int textX = globalBounds.X + (globalBounds.Width - textWidth) / 2;
				int textY = globalBounds.Y + TextMargin;

				_font.drawText(spriteBatch, text,
					new Vector2(textX, textY), TextColor, Color.Black);
			}
		}
	}
}