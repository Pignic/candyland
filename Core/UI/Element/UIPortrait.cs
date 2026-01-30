using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIPortrait : UIElement {
	private readonly Dictionary<string, Texture2D> _portraits;
	private string _currentPortraitKey;

	public UIPortrait() : base() {
		_portraits = [];
		_currentPortraitKey = "default";

		BackgroundColor = Color.Transparent;
		BorderColor = Color.Gold;
		BorderWidth = 2;
	}

	public void LoadPortrait(string key, Texture2D texture) {
		if (texture != null) {
			_portraits[key] = texture;
		}
	}

	public void SetPortrait(string portraitKey) {
		_currentPortraitKey = portraitKey ?? "default";
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle bounds = GlobalBounds;

		// Draw portrait if available
		if (_portraits.TryGetValue(_currentPortraitKey, out Texture2D portrait)) {
			spriteBatch.Draw(portrait, bounds, Color.White);
		}
	}
}