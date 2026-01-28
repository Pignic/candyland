using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI;

public class UIProgressBar : UIElement {

	public Func<string> GetText { get; set; }
	public Func<float> GetValue { get; set; } // Returns 0-1

	// Styling
	public Color ForegroundColor { get; set; } = Color.Red;
	public Color TextColor { get; set; } = Color.White;
	public int TextMargin { get; set; } = 2;

	public UIProgressBar(Func<string> getText, Func<float> getValue) : base() {
		GetText = getText;
		GetValue = getValue;

		// Default size
		Height = Font.GetHeight(TextMargin);
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Border
		if (BorderWidth > 0) {
			Rectangle borderBounds = new Rectangle(
				globalBounds.X - BorderWidth,
				globalBounds.Y - BorderWidth,
				globalBounds.Width + (BorderWidth * 2),
				globalBounds.Height + (BorderWidth * 2)
			);
			spriteBatch.Draw(DefaultTexture, borderBounds, BorderColor);
		}

		// Background (empty portion)
		spriteBatch.Draw(DefaultTexture, globalBounds, BackgroundColor);

		// Foreground (filled portion)
		float value = MathHelper.Clamp(GetValue(), 0f, 1f);
		int filledWidth = (int)(globalBounds.Width * value);

		Rectangle filledBounds = new Rectangle(
			globalBounds.X,
			globalBounds.Y,
			filledWidth,
			globalBounds.Height
		);
		spriteBatch.Draw(DefaultTexture, filledBounds, ForegroundColor);

		// Text (centered)
		string text = GetText();
		if (!string.IsNullOrEmpty(text)) {
			int textWidth = Font.MeasureString(text);
			int textX = globalBounds.X + ((globalBounds.Width - textWidth) / 2);
			int textY = globalBounds.Y + TextMargin;

			Font.DrawText(spriteBatch, text,
				new Vector2(textX, textY), TextColor, Color.Black);
		}
	}
}