using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI.Element;

public class UIImage : UIElement {

	private readonly Texture2D _image;

	public UIImage(Texture2D image) {
		_image = image;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		spriteBatch.Draw(_image, GlobalBounds, Color.White);
	}
}
