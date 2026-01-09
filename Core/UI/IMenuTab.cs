using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Core.UI;

public interface IMenuTab {

	UIPanel RootPanel { get; }

	bool IsVisible { get; set; }

	void Initialize();

	void RefreshContent();

	void Update(GameTime gameTime);

	void HandleMouse(MouseState mouseState, MouseState previousMouseState);

	void Draw(SpriteBatch spriteBatch);

	int GetNavigableCount();

	UIElement GetNavigableElement(int index);

	void Dispose();
}