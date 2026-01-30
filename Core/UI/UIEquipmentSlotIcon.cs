using EldmeresTale.Core.UI.Element;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EldmeresTale.Core.UI;

public class UIEquipmentSlotIcon : UIElement {
	private readonly EquipmentSlot _slot;
	private readonly Equipment _equipped;

	public Action OnClick { get; set; }
	public Action<bool, UIElement> OnHover { get; set; }

	private bool _isHovered = false;

	// Slot size - square
	private const int SLOT_SIZE = 32;

	public UIEquipmentSlotIcon(EquipmentSlot slot, Equipment equipped) : base() {
		_slot = slot;
		_equipped = equipped;

		Width = SLOT_SIZE;
		Height = SLOT_SIZE;

		// Set border based on equipped status (before drawing!)
		if (_equipped != null) {
			BorderWidth = 2;
			BorderColor = _equipped.GetRarityColor();
		} else {
			BorderWidth = 1;
			BorderColor = Color.DarkGray;
		}
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Draw slot background (dark)
		spriteBatch.Draw(DefaultTexture, globalBounds, new Color(20, 20, 20));

		// Draw item icon if equipped
		if (_equipped?.Icon != null) {
			// Draw item icon (if equipment has Icon texture)
			spriteBatch.Draw(_equipped.Icon, globalBounds, Color.White);
		} else if (_equipped != null) {
			// Item equipped but no icon - show item name initial
			string initial = _equipped.Name.Substring(0, 1).ToUpper();
			int textWidth = Font.MeasureString(initial);
			int textHeight = Font.GetHeight();
			int textX = globalBounds.X + ((globalBounds.Width - textWidth) / 2);
			int textY = globalBounds.Y + ((globalBounds.Height - textHeight) / 2);

			Font.DrawText(spriteBatch, initial,
				new Vector2(textX, textY),
				_equipped.GetRarityColor());
		} else {
			// Empty slot - show slot type placeholder
			DrawSlotPlaceholder(spriteBatch, globalBounds);
		}

		// Highlight on hover
		if (_isHovered) {
			spriteBatch.Draw(DefaultTexture, globalBounds, Color.White * 0.3f);
		}
	}

	private void DrawSlotPlaceholder(SpriteBatch spriteBatch, Rectangle bounds) {
		// Draw a simple symbol representing the slot type
		string symbol = _slot switch {
			EquipmentSlot.Helmet => "H",
			EquipmentSlot.Amulet => "A",
			EquipmentSlot.Armor => "C",
			EquipmentSlot.Gloves => "G",
			EquipmentSlot.Belt => "B",
			EquipmentSlot.Pants => "P",
			EquipmentSlot.Boots => "F",
			EquipmentSlot.Ring => "R",
			EquipmentSlot.Weapon => "W",
			_ => "?"
		};

		int textWidth = Font.MeasureString(symbol);
		int textHeight = Font.GetHeight();
		int textX = bounds.X + ((bounds.Width - textWidth) / 2);
		int textY = bounds.Y + ((bounds.Height - textHeight) / 2);

		Font.DrawText(spriteBatch, symbol,
			new Vector2(textX, textY),
			Color.DarkGray * 0.5f);
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		Point mousePos = mouse.Position;
		bool nowHovered = GlobalBounds.Contains(mousePos);

		if (nowHovered != _isHovered) {
			_isHovered = nowHovered;
			OnHover?.Invoke(_isHovered, this);
		}

		// Only clickable if something is equipped (right-click to unequip)
		if (_equipped != null && _isHovered) {
			if (mouse.LeftButton == ButtonState.Pressed &&
				previousMouse.LeftButton == ButtonState.Released) {
				OnClick?.Invoke();
				return true;
			}
		}

		return _isHovered;
	}
}