using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace EldmeresTale.Core.UI;

public abstract class UINavigableElement : UIElement {

	public Action OnClick { get; set; }
	public Action<bool, UIElement> OnHover { get; set; }

	protected bool _forceHover = false;
	protected bool _isMouseHovered = false;
	public bool IsHovered => _forceHover || _isMouseHovered;

	public new bool Enabled {
		get => base.Enabled;
		set {
			base.Enabled = value;
			// Clear states when disabled
			if (!value) {
				_isMouseHovered = false;
				_forceHover = false;
			}
		}
	}

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

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		Point mousePos = mouse.Position;
		bool nowHovered = GlobalBounds.Contains(mousePos);

		if (nowHovered != IsHovered) {
			_isMouseHovered = nowHovered;
			OnHover?.Invoke(IsHovered, this);
		}

		if (_isMouseHovered && mouse.LeftButton == ButtonState.Pressed &&
			previousMouse.LeftButton == ButtonState.Released) {
			OnClick?.Invoke();
			return true;
		}

		return IsHovered;
	}

}
