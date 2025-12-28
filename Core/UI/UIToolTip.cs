using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI;

internal class UIToolTip : UIComponent {

	public object tooltipObject;
	public Action<object, UIToolTip, SpriteBatch> renderContent { get; set; }

	public UIToolTip(BitmapFont font, int x, int y) : base(font, x, y) {

	}

	public override void draw(SpriteBatch spriteBatch) {
		renderContent(tooltipObject, this, spriteBatch);
	}
}
