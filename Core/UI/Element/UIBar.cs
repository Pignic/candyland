using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI.Element;

public class UIBar : UIElement {

	public int TextMargin { get; set; }
	public Func<string> GetText { get; set; }
	public Func<float> GetValue { get; set; }
	public Color FgColor { get; set; }
	public Color TextColor { get; set; }

	public UIBar(int x, int y, int width, int textMargin, Color bgColor, Color fgColor, Color borderColor,
			Color textColor, Func<string> getText, Func<float> getValue) : base() {
		X = x;
		Y = y;
		Width = width;
		GetText = getText;
		GetValue = getValue;
		TextMargin = textMargin;
		BorderWidth = 2;
		Height = Font.GetHeight(TextMargin) + (BorderWidth * 2);
		BackgroundColor = bgColor;
		FgColor = fgColor;
		BorderColor = borderColor;
		TextColor = textColor;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		base.Draw(spriteBatch);

		// Foreground (current value)
		int currentWidth = (int)(GetValue() * (Width - (BorderWidth * 2)));
		Rectangle contentBar = GlobalContentBounds;
		contentBar.Width = currentWidth;
		spriteBatch.Draw(DefaultTexture, contentBar, FgColor);

		// Draw text centered on the bar
		string text = GetText();
		int textWidth = Font.MeasureString(text);
		int textX = X + ((Width - textWidth) / 2);
		int textY = Y + BorderWidth + TextMargin;

		Font.DrawText(spriteBatch, text, new Vector2(textX, textY), TextColor, Color.Black);
	}
}