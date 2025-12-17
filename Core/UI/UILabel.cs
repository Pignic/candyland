using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.Core.UI; 

public class UILabel : UIElement {
	private BitmapFont _font;

	private Func<string> _textFunc;
	public Color TextColor { get; set; } = Color.White;
	public Color? ShadowColor { get; set; } = Color.Black;
	public Point? ShadowOffset { get; set; } = new Point(1, 1);

	public enum TextAlignment {
		Left,
		Center,
		Right
	}

	public TextAlignment Alignment { get; set; } = TextAlignment.Left;

	public UILabel(BitmapFont font, string text = "", Func<string> textFunc = null) {
		_font = font;
		if(textFunc != null) {
			_textFunc = textFunc;
		} else {
			_textFunc = () => text;
		}
		UpdateSize();
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		string text = _textFunc();
		if(string.IsNullOrEmpty(text)) return;

		var globalPos = GlobalPosition;
		int textWidth = _font.measureString(text);
		int xOffset = 0;

		// Calculate alignment offset
		switch(Alignment) {
			case TextAlignment.Center:
				xOffset = (Width - textWidth) / 2;
				break;
			case TextAlignment.Right:
				xOffset = Width - textWidth;
				break;
		}

		Vector2 position = new Vector2(globalPos.X + xOffset, globalPos.Y);
		_font.drawText(spriteBatch, text, position, TextColor, ShadowColor, ShadowOffset);
	}

	public void UpdateSize() {
		string text = _textFunc();
		if(!string.IsNullOrEmpty(text)) {
			Width = _font.measureString(text);
			Height = _font.getHeight();
		}
	}

	public void SetText(string text) {
		_textFunc = () => text;
		UpdateSize();
	}

	public void SetTextFunction(Func<string> textFunc) {
		_textFunc = textFunc;
		UpdateSize();
	}
}