using EldmeresTale.Core.UI.Element;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EldmeresTale.Core.UI;

public class UIMaterialGridItem : UIElement {
	private readonly string _materialId;
	private int _quantity;
	private readonly Texture2D _icon;
	private bool _isHovered;
	private bool _isSelected;

	public string MaterialId => _materialId;
	public int Quantity => _quantity;

	public event Action<UIMaterialGridItem> OnClicked;
	public event Action<UIMaterialGridItem> OnHoverEnter;
	public event Action<UIMaterialGridItem> OnHoverExit;

	private const int ICON_SIZE = 32; // Display size (scaled from 16x16)

	public UIMaterialGridItem(string materialId, Texture2D icon) {
		_materialId = materialId;
		_quantity = 0;
		Width = ICON_SIZE + 8;
		Height = ICON_SIZE + 8;
		BackgroundColor = new Color(60, 60, 60);
		BorderColor = Color.Gray;
		BorderWidth = 2;
		_icon = icon;
	}

	public void SetQuantity(int quantity) {
		_quantity = quantity;
	}

	public void SetSelected(bool selected) {
		_isSelected = selected;
		BorderColor = selected ? Color.Yellow : Color.Gray;
		BorderWidth = selected ? 2 : 1;
	}

	public override bool HandleMouse(MouseState mouse, MouseState previousMouse) {
		if (!Visible || !Enabled) {
			return false;
		}

		bool isInBounds = GlobalBounds.Contains(mouse.Position);

		// Hover state
		if (isInBounds && !_isHovered) {
			_isHovered = true;
			BackgroundColor = new Color(80, 80, 80);
			OnHoverEnter?.Invoke(this);
		} else if (!isInBounds && _isHovered) {
			_isHovered = false;
			BackgroundColor = new Color(60, 60, 60);
			OnHoverExit?.Invoke(this);
		}

		// Click detection
		if (isInBounds &&
			mouse.LeftButton == ButtonState.Pressed &&
			previousMouse.LeftButton == ButtonState.Released) {
			OnClicked?.Invoke(this);
			return true;
		}

		return false;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Point globalPos = GlobalPosition;

		// Draw icon (centered)
		int iconX = globalPos.X + ((Width - ICON_SIZE) / 2);
		int iconY = globalPos.Y + ((Height - ICON_SIZE) / 2);

		Rectangle iconRect = new Rectangle(iconX, iconY, ICON_SIZE, ICON_SIZE);
		spriteBatch.Draw(_icon, iconRect, Color.White);

		// Draw quantity text (bottom-left overlay)
		string quantityText = $"x{_quantity}";
		Vector2 textPos = new Vector2(
			globalPos.X + 2,
			globalPos.Y + Height - Font.GetHeight() - 2
		);

		// Text shadow for readability
		Font.DrawText(spriteBatch, quantityText, textPos + Vector2.One, Color.Black);
		Font.DrawText(spriteBatch, quantityText, textPos, Color.White);
	}
}