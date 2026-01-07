using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core;

public static class Graphics {

	public static Texture2D CreateColoredTexture(GraphicsDevice graphicsDevice, int width, int height, Color color) {
		Texture2D texture = new Texture2D(graphicsDevice, width, height);
		Color[] colorData = new Color[width * height];
		for (int i = 0; i < colorData.Length; i++) {
			colorData[i] = color;
		}

		texture.SetData(colorData);
		return texture;
	}
}
