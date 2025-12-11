namespace Candyland.Core.UI;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


public class UIBar : UIComponent {

	private Texture2D bgTexture;
	public int width { get; set; }
	public int textMargin { get; set; }
	public Func<string> getText { get; set; }
	public Func<float> getValue { get; set; }
	public Color bgColor { get; set; }
	public Color fgColor { get; set; }
	public Color borderColor { get; set; }
	public Color textColor { get; set; }

	public UIBar(GraphicsDevice graphicsDevice, BitmapFont font, int x, int y, int width, int textMargin, Color bgColor, Color fgColor, Color borderColor,
			Color textColor, Func<string> getText, Func<float> getValue) : base(font, x, y) {
		this.bgTexture = Graphics.CreateColoredTexture(graphicsDevice, 1, 1, Color.White);
		this.getText = getText;
		this.getValue = getValue;
		this.width = width;
		this.textMargin = textMargin;
		this.bgColor = bgColor;
		this.fgColor = fgColor;
		this.borderColor = borderColor;
		this.textColor = textColor;
	}

	public override void draw(SpriteBatch spriteBatch) {
		int barHeight = font.getHeight(textMargin);

		// Border
		spriteBatch.Draw(bgTexture, new Rectangle(x - 2, y - 2, width + 4, barHeight + 4), borderColor);

		// Background (empty value)
		spriteBatch.Draw(bgTexture, new Rectangle(x, y, width, barHeight), bgColor);

		// Foreground (current value)
		int currentWidth = (int)(getValue() * width);
		spriteBatch.Draw(bgTexture, new Rectangle(x, y, currentWidth, barHeight), fgColor);

		// Draw text centered on the bar
		string text = getText();
		int textWidth = font.measureString(text);
		int textX = x + (width - textWidth) / 2;
		int textY = y + textMargin;

		font.drawText(spriteBatch, text, new Vector2(textX, textY), textColor, Color.Black);
	}
}