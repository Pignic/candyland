namespace EldmeresTale.Core.UI;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class UICounter : UIComponent {
	public Func<string> getValue { get; set; }
	public int textMargin { get; set; }
	public Color textColor { get; set; }
	public string label { get; set; }

	public UICounter(BitmapFont font, int x, int y, int textMargin, Color textColor, string label, Func<string> getValue) :base(font, x, y)  {
		this.textMargin = textMargin;
		this.textColor = textColor;
		this.label = label;
		this.getValue = getValue;
	}

	public override void draw(SpriteBatch spriteBatch) {
		int height = font.getHeight(textMargin);

		// Draw text centered on the bar
		string text = getValue();
		int textWidth = font.measureString(label);
		int textX = x + textWidth + textMargin;
		int textY = y + textMargin;

		font.drawText(spriteBatch, label, new Vector2(x, textY), textColor, Color.Black);
		font.drawText(spriteBatch, text, new Vector2(textX, textY), textColor, Color.Black);
	}
}
