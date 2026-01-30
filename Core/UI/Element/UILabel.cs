using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI.Element;

public class UILabel : UIElement {
	private Func<string> _textFunc;
	public string Text { get; set; } // Direct text property
	public Color TextColor { get; set; } = Color.White;
	public Color? ShadowColor { get; set; } = Color.Black;
	public Point? ShadowOffset { get; set; } = new Point(1, 1);
	public float Scale { get; set; } = 1f;
	public bool Centered { get; set; } = false;
	public bool WordWrap { get; set; } = false;

	public enum TextAlignment {
		Left,
		Center,
		Right
	}

	public TextAlignment Alignment { get; set; } = TextAlignment.Left;

	public UILabel(string text = "", Func<string> textFunc = null) : base() {
		Text = text;
		_textFunc = textFunc;
		UpdateSize();
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		string text = _textFunc != null ? _textFunc() : Text;
		if (string.IsNullOrEmpty(text)) {
			return;
		}

		Point globalPos = GlobalPosition;

		if (WordWrap && Width > 0) {
			DrawWrappedText(spriteBatch, text, globalPos);
		} else {
			DrawSingleLineText(spriteBatch, text, globalPos);
		}
	}

	private void DrawSingleLineText(SpriteBatch spriteBatch, string text, Point globalPos) {
		int textWidth = Font.MeasureString(text, Scale);
		int xOffset = 0;

		switch (Alignment) {
			case TextAlignment.Center:
				xOffset = (Width - textWidth) / 2;
				break;
			case TextAlignment.Right:
				xOffset = Width - textWidth;
				break;
		}

		Vector2 position = new Vector2(globalPos.X + xOffset + PaddingLeft, globalPos.Y + PaddingTop);
		Font.DrawText(spriteBatch, text, position, TextColor, ShadowColor, ShadowOffset, Scale, Centered);
	}

	private void DrawWrappedText(SpriteBatch spriteBatch, string text, Point globalPos) {
		int lineHeight = Font.GetHeight(1, Scale);
		int currentY = globalPos.Y + PaddingTop;
		string[] words = text.Split(' ');
		string currentLine = "";

		foreach (string word in words) {
			string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
			int lineWidth = Font.MeasureString(testLine, Scale);

			if (lineWidth > Width && !string.IsNullOrEmpty(currentLine)) {
				// Draw current line
				Font.DrawText(spriteBatch, currentLine, new Vector2(globalPos.X, currentY), TextColor, ShadowColor, ShadowOffset, Scale, false);
				currentY += lineHeight;
				currentLine = word;

				// Stop if we exceed height
				if (Height > 0 && currentY + lineHeight > globalPos.Y + Height) {
					break;
				}
			} else {
				currentLine = testLine;
			}
		}

		// Draw remaining text
		if (!string.IsNullOrEmpty(currentLine)) {
			Font.DrawText(spriteBatch, currentLine, new Vector2(globalPos.X + PaddingLeft, currentY), TextColor, ShadowColor, ShadowOffset, Scale, false);
		}
	}

	public void UpdateSize() {
		string text = _textFunc != null ? _textFunc() : Text;
		if (string.IsNullOrEmpty(text)) {
			return;
		}

		if (WordWrap && Width > 0) {
			// Calculate height based on wrapped lines
			int lineHeight = Font.GetHeight(1, Scale);
			string[] words = text.Split(' ');
			string currentLine = "";
			int lineCount = 1;

			foreach (string word in words) {
				string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
				int lineWidth = Font.MeasureString(testLine, Scale);

				if (lineWidth > Width && !string.IsNullOrEmpty(currentLine)) {
					lineCount++;
					currentLine = word;
				} else {
					currentLine = testLine;
				}
			}

			Height = lineCount * lineHeight;
		} else {
			Width = Font.MeasureString(text, Scale);
			Height = Font.GetHeight(1, Scale);
		}
	}

	public void SetText(string text) {
		Text = text;
		_textFunc = null;
		UpdateSize();
	}

	public void SetTextFunction(Func<string> textFunc) {
		_textFunc = textFunc;
		UpdateSize();
	}
}