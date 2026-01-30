using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI.Element;

internal class UIToolTip : UIPanel {

	public object tooltipObject;

	public Action<object, UIToolTip, SpriteBatch> RenderContent { get; set; }

	public UIToolTip(int x, int y) : base() {
		X = x;
		Y = y;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		RenderContent(tooltipObject, this, spriteBatch);
	}
}
