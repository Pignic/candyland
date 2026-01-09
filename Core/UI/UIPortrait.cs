using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIPortrait : UIElement {
	private readonly Dictionary<string, Texture2D> _portraits;
	private string _currentPortraitKey;

	public UIPortrait() : base() {
		_portraits = [];
		_currentPortraitKey = "default";

		BackgroundColor = Color.Transparent;
		BorderColor = Color.Gold;
		BorderWidth = 2;
	}

	public void LoadPortrait(string key, Texture2D texture) {
		if (texture != null) {
			_portraits[key] = texture;
		}
	}

	public void SetPortrait(string portraitKey) {
		_currentPortraitKey = portraitKey ?? "default";
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle bounds = GlobalBounds;

		// Draw portrait if available
		if (_portraits.TryGetValue(_currentPortraitKey, out Texture2D portrait)) {
			spriteBatch.Draw(portrait, bounds, Color.White);
		} else {
			// Draw placeholder
			DrawPlaceholder(spriteBatch, bounds);
		}
	}

	private void DrawPlaceholder(SpriteBatch spriteBatch, Rectangle bounds) {
		// Generate color based on portrait key
		Color placeholderColor = GetColorFromString(_currentPortraitKey);
		spriteBatch.Draw(_defaultTexture, bounds, placeholderColor);

		// Draw simple face
		int centerX = bounds.X + (bounds.Width / 2);
		int centerY = bounds.Y + (bounds.Height / 2);

		// Eyes
		Rectangle leftEye = new Rectangle(centerX - 12, centerY - 8, 6, 6);
		Rectangle rightEye = new Rectangle(centerX + 6, centerY - 8, 6, 6);
		spriteBatch.Draw(_defaultTexture, leftEye, Color.Black);
		spriteBatch.Draw(_defaultTexture, rightEye, Color.Black);

		// Mouth (simple line)
		Rectangle mouth = new Rectangle(centerX - 10, centerY + 8, 20, 2);
		spriteBatch.Draw(_defaultTexture, mouth, Color.Black);
	}

	private static Color GetColorFromString(string str) {
		if (string.IsNullOrEmpty(str)) {
			return Color.Gray;
		}

		int hash = str.GetHashCode();
		byte r = (byte)((hash & 0xFF0000) >> 16);
		byte g = (byte)((hash & 0x00FF00) >> 8);
		byte b = (byte)(hash & 0x0000FF);

		// Ensure reasonable brightness
		if (r + g + b < 150) {
			r = (byte)(r + 100);
			g = (byte)(g + 100);
			b = (byte)(b + 100);
		}

		return new Color(r, g, b);
	}
}