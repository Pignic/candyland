using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

public abstract class UIComponent {
	public BitmapFont font { get; set; }
	public int x { get; set; }
	public int y { get; set; }

	public UIComponent(BitmapFont font, int x, int y) {
		this.font = font;
		this.x = x;
		this.y = y;
	}

	public abstract void draw(SpriteBatch spriteBatch);

}
