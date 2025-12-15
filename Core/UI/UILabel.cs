using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Candyland.Core.UI {
	/// <summary>
	/// Simple text label
	/// </summary>
	public class UILabel : UIElement {
		private BitmapFont _font;

		public string Text { get; set; }
		public Color TextColor { get; set; } = Color.White;
		public Color? ShadowColor { get; set; } = Color.Black;
		public Point? ShadowOffset { get; set; } = new Point(1, 1);

		public enum TextAlignment {
			Left,
			Center,
			Right
		}

		public TextAlignment Alignment { get; set; } = TextAlignment.Left;

		public UILabel(BitmapFont font, string text = "") {
			_font = font;
			Text = text;
			UpdateSize();
		}

		protected override void OnDraw(SpriteBatch spriteBatch) {
			if(string.IsNullOrEmpty(Text)) return;

			var globalPos = GlobalPosition;
			int textWidth = _font.measureString(Text);
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
			_font.drawText(spriteBatch, Text, position, TextColor, ShadowColor, ShadowOffset);
		}

		/// <summary>
		/// Update width/height based on text content
		/// </summary>
		public void UpdateSize() {
			if(!string.IsNullOrEmpty(Text)) {
				Width = _font.measureString(Text);
				Height = _font.getHeight();
			}
		}

		/// <summary>
		/// Set text and auto-update size
		/// </summary>
		public void SetText(string text) {
			Text = text;
			UpdateSize();
		}
	}
}