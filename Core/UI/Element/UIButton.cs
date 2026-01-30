using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI.Element;

public class UIButton : UINavigableElement {

	public string Text { get; set; }

	public void Click() {
		OnClick?.Invoke();
	}

	// Styling
	public Color HoverColor { get; set; } = new Color(80, 80, 80);
	public Color HoverTextColor { get; set; } = new Color(180, 180, 0);
	public Color PressedColor { get; set; } = new Color(40, 40, 40);
	public Color TextColor { get; set; } = Color.White;

	// State
	private bool _isPressed = false;

	public new bool Enabled {
		get => base.Enabled;
		set {
			base.Enabled = value;
			// Clear states when disabled
			if (!value) {
				_isPressed = false;
			}
		}
	}

	public enum TextAlignment {
		Left,
		Center,
		Right
	}
	public TextAlignment Alignment { get; set; } = TextAlignment.Center;
	public int TextPadding { get; set; } = 5;

	public UIButton(string text) : base() {
		Text = text;
		// Default size based on text
		int textWidth = Font.MeasureString(text);
		int textHeight = Font.GetHeight();
		Width = textWidth + 20;
		Height = textHeight + 10;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Determine background color based on state
		Color bgColor = BackgroundColor;
		if (!Enabled) {
			bgColor = BackgroundColor * 0.5f;
		} else if (_isPressed) {
			bgColor = PressedColor;
		} else if (IsHovered) {
			bgColor = HoverColor;
		}

		// Background
		spriteBatch.Draw(DefaultTexture, globalBounds, bgColor);

		// Text with alignment
		if (!string.IsNullOrEmpty(Text)) {
			int textWidth = Font.MeasureString(Text);
			int textHeight = Font.GetHeight();
			int textY = globalBounds.Y + ((globalBounds.Height - textHeight) / 2);
			int textX = Alignment switch {
				TextAlignment.Left => globalBounds.X + TextPadding,
				TextAlignment.Right => globalBounds.X + globalBounds.Width - textWidth - TextPadding,
				_ => globalBounds.X + ((globalBounds.Width - textWidth) / 2),
			};
			Font.DrawText(spriteBatch, Text, new Vector2(textX, textY),
				IsHovered ? HoverTextColor : TextColor);
		}
	}
}