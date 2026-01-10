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
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Draw slot background (dark)
		spriteBatch.Draw(DefaultTexture, globalBounds, new Color(20, 20, 20));

		// Draw item icon if equipped
		if (_equipped?.Icon != null) {
			// Draw item icon
			spriteBatch.Draw(_equipped.Icon, globalBounds, Color.White);
		} else {
			// Draw slot type indicator (empty slot)
			DrawSlotPlaceholder(spriteBatch, globalBounds);
		}

		// Draw rarity border if equipped
		if (_equipped != null) {
			Color rarityColor = _equipped.GetRarityColor();
			BorderWidth = 2;
			BorderColor = rarityColor;
		} else {
			// Empty slot border (gray)
			BorderWidth = 1;
			BorderColor = Color.DarkGray;
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
			if (mouse.RightButton == ButtonState.Pressed &&
				previousMouse.RightButton == ButtonState.Released) {
				OnClick?.Invoke();
				return true;
			}
		}

		return _isHovered;
	}
}