using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.Core.UI;

public class UIPanel : UIComponent {

	public int width { get; set; }
	public int height { get; set; }

	private int SCROLLBAR_WIDTH = 10;

	private Rectangle contentArea;

	public UIPanel(BitmapFont font, int x, int y, int width, int height) : base(font, x, y) {
		this.width = width;
		this.height = height;
		contentArea = new Rectangle(this.x, this.y, width, height);
	}

	public override void draw(SpriteBatch spriteBatch) {
		Rectangle scissorRect = new Rectangle(
			contentArea.X,
			contentArea.Y,
			contentArea.Width + SCROLLBAR_WIDTH + 10, // Include scrollbar area
			contentArea.Height
		);
	}
}
