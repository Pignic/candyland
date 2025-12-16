using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Candyland.Core.UI;

public class UIPortrait {

	private Rectangle bounds;
	private Texture2D pixelTexture;
	private Dictionary<string, Texture2D> portraits;

	public UIPortrait(Rectangle bounds, Texture2D pixelTexture) {
		this.bounds = bounds;
		this.pixelTexture = pixelTexture;
		portraits = new Dictionary<string, Texture2D>();
	}

	public void loadPortrait(string key, Texture2D texture) {
		portraits[key] = texture;
	}

	public void draw(SpriteBatch spriteBatch, string portraitKey) {
		// Draw border/frame
		drawFrame(spriteBatch);

		// Draw portrait if available
		if(portraits.ContainsKey(portraitKey)) {
			Texture2D portrait = portraits[portraitKey];
			spriteBatch.Draw(portrait, bounds, Color.White);
		} else {
			// Draw placeholder if no portrait loaded
			drawPlaceholder(spriteBatch, portraitKey);
		}
	}

	private void drawFrame(SpriteBatch spriteBatch) {
		int thickness = 2;
		// Top
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X - thickness, bounds.Y - thickness, bounds.Width + thickness * 2, thickness), Color.Gold);
		// Bottom
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X - thickness, bounds.Bottom, bounds.Width + thickness * 2, thickness), Color.Gold);
		// Left
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X - thickness, bounds.Y - thickness, thickness, bounds.Height + thickness * 2), Color.Gold);
		// Right
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right, bounds.Y - thickness, thickness, bounds.Height + thickness * 2), Color.Gold);
	}

	private void drawPlaceholder(SpriteBatch spriteBatch, string portraitKey) {
		// Generate a color based on the portrait key
		Color placeholderColor = getColorFromString(portraitKey);

		// Draw colored background
		spriteBatch.Draw(pixelTexture, bounds, placeholderColor);

		// Draw simple face placeholder (optional)
		drawSimpleFace(spriteBatch);
	}

	private void drawSimpleFace(SpriteBatch spriteBatch) {
		int centerX = bounds.X + bounds.Width / 2;
		int centerY = bounds.Y + bounds.Height / 2;

		// Head circle (approximated with rectangles)
		int headSize = bounds.Width / 2;
		Rectangle head = new Rectangle(
			centerX - headSize / 2,
			centerY - headSize / 2,
			headSize,
			headSize
		);
		spriteBatch.Draw(pixelTexture, head, Color.White * 0.3f);

		// Eyes
		int eyeSize = 8;
		int eyeOffset = 15;
		Rectangle leftEye = new Rectangle(centerX - eyeOffset, centerY - 10, eyeSize, eyeSize);
		Rectangle rightEye = new Rectangle(centerX + eyeOffset - eyeSize, centerY - 10, eyeSize, eyeSize);
		spriteBatch.Draw(pixelTexture, leftEye, Color.Black);
		spriteBatch.Draw(pixelTexture, rightEye, Color.Black);

		// Mouth
		Rectangle mouth = new Rectangle(centerX - 12, centerY + 10, 24, 3);
		spriteBatch.Draw(pixelTexture, mouth, Color.Black);
	}

	private Color getColorFromString(string text) {
		if(string.IsNullOrEmpty(text)){
			return Color.Gray;
		}

		// Simple hash to color conversion
		int hash = text.GetHashCode();
		byte r = (byte)((hash & 0xFF0000) >> 16);
		byte g = (byte)((hash & 0x00FF00) >> 8);
		byte b = (byte)(hash & 0x0000FF);

		// Ensure colors are not too dark
		r = (byte)(r / 2 + 128);
		g = (byte)(g / 2 + 128);
		b = (byte)(b / 2 + 128);

		return new Color(r, g, b);
	}
}