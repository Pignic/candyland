using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Core.UI;

public abstract class UINavigableElement : UIElement {

	protected bool _forceHover = false;
	protected bool _isMouseHovered = false;
	public bool IsHovered => _forceHover || _isMouseHovered;

	protected UINavigableElement() : base() {
		IsNavigable = true;
	}

	public void ForceHoverState(bool hovered) {
		_forceHover = hovered;
	}

	protected void UpdateMouseHover(MouseState mouseState) {
		Point mousePos = mouseState.Position;
		_isMouseHovered = Enabled && GlobalBounds.Contains(mousePos) && Visible;
	}

}
