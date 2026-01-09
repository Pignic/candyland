namespace EldmeresTale.Core.UI;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class UICounter : UIComponent {

	public Func<string> GetValue { get; set; }
	public int TextMargin { get; set; }
	public Color TextColor { get; set; }
	public string Label { get; set; }

	public UICounter(int x, int y, int textMargin, Color textColor, string label, Func<string> getValue) : base(x, y) {
		TextMargin = textMargin;
		TextColor = textColor;
		Label = label;
		GetValue = getValue;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		int height = Font.GetHeight(TextMargin);

		// Draw text centered on the bar
		string text = GetValue();
		int textWidth = Font.MeasureString(Label);
		int textX = X + textWidth + TextMargin;
		int textY = Y + TextMargin;

		Font.DrawText(spriteBatch, Label, new Vector2(X, textY), TextColor, Color.Black);
		Font.DrawText(spriteBatch, text, new Vector2(textX, textY), TextColor, Color.Black);
	}
}
