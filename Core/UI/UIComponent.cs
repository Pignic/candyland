using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

public abstract class UIComponent {

	public BitmapFont Font { get; set; }
	public int X { get; set; }
	public int Y { get; set; }

	protected UIComponent(BitmapFont font, int x, int y) {
		Font = font;
		X = x;
		Y = y;
	}

	public abstract void Draw(SpriteBatch spriteBatch);

}
