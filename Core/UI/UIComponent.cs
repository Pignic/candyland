using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

public abstract class UIComponent {

	public static void SetGraphicContext(GraphicsDevice graphicsDevice) {
		FONT = new BitmapFont(graphicsDevice);
	}
	private static BitmapFont FONT;

	public BitmapFont Font => FONT;
	public int X { get; set; }
	public int Y { get; set; }

	protected UIComponent(int x, int y) {
		X = x;
		Y = y;
	}

	public abstract void Draw(SpriteBatch spriteBatch);
}
