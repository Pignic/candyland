using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI;

internal class UIToolTip : UIComponent {

	public object tooltipObject;

	public Action<object, UIToolTip, SpriteBatch> RenderContent { get; set; }

	public UIToolTip(BitmapFont font, int x, int y) : base(font, x, y) {

	}

	public override void Draw(SpriteBatch spriteBatch) {
		RenderContent(tooltipObject, this, spriteBatch);
	}
}
