using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI;


public class UIBar : UIComponent {

	private readonly Texture2D bgTexture;
	public int Width { get; set; }
	public int TextMargin { get; set; }
	public Func<string> GetText { get; set; }
	public Func<float> GetValue { get; set; }
	public Color BgColor { get; set; }
	public Color FgColor { get; set; }
	public Color BorderColor { get; set; }
	public Color TextColor { get; set; }

	public UIBar(GraphicsDevice graphicsDevice, int x, int y, int width, int textMargin, Color bgColor, Color fgColor, Color borderColor,
			Color textColor, Func<string> getText, Func<float> getValue) : base(x, y) {
		bgTexture = Graphics.CreateColoredTexture(graphicsDevice, 1, 1, Color.White);
		GetText = getText;
		GetValue = getValue;
		Width = width;
		TextMargin = textMargin;
		BgColor = bgColor;
		FgColor = fgColor;
		BorderColor = borderColor;
		TextColor = textColor;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		int barHeight = Font.GetHeight(TextMargin);

		// Border
		spriteBatch.Draw(bgTexture, new Rectangle(X - 2, Y - 2, Width + 4, barHeight + 4), BorderColor);

		// Background (empty value)
		spriteBatch.Draw(bgTexture, new Rectangle(X, Y, Width, barHeight), BgColor);

		// Foreground (current value)
		int currentWidth = (int)(GetValue() * Width);
		spriteBatch.Draw(bgTexture, new Rectangle(X, Y, currentWidth, barHeight), FgColor);

		// Draw text centered on the bar
		string text = GetText();
		int textWidth = Font.MeasureString(text);
		int textX = X + ((Width - textWidth) / 2);
		int textY = Y + TextMargin;

		Font.DrawText(spriteBatch, text, new Vector2(textX, textY), TextColor, Color.Black);
	}
}