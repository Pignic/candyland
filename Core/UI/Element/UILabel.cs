using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI;

public class UILabel : UIElement {

	private Func<string> _textFunc;
	public Color TextColor { get; set; } = Color.White;
	public Color? ShadowColor { get; set; } = Color.Black;
	public Point? ShadowOffset { get; set; } = new Point(1, 1);
	public float Scale { get; set; } = 1f;
	public bool Centered { get; set; } = false;
	public bool WorldWrap { get; set; } = false;

	public enum TextAlignment {
		Left,
		Center,
		Right
	}

	public TextAlignment Alignment { get; set; } = TextAlignment.Left;

	public UILabel(string text = "", Func<string> textFunc = null) : base() {
		_textFunc = textFunc ?? (() => text);
		UpdateSize();
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		string text = _textFunc();
		if (string.IsNullOrEmpty(text)) {
			return;
		}

		Point globalPos = GlobalPosition;
		int textWidth = Font.MeasureString(text, Scale);
		int xOffset = 0;

		// Calculate alignment offset
		switch (Alignment) {
			case TextAlignment.Center:
				xOffset = (Width - textWidth) / 2;
				break;
			case TextAlignment.Right:
				xOffset = Width - textWidth;
				break;
		}

		Vector2 position = new Vector2(globalPos.X + xOffset, globalPos.Y);
		Font.DrawText(spriteBatch, text, position, TextColor, ShadowColor, ShadowOffset, Scale, Centered);
	}

	public void UpdateSize() {
		string text = _textFunc();
		if (!string.IsNullOrEmpty(text)) {
			Width = Font.MeasureString(text);
			Height = Font.GetHeight();
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