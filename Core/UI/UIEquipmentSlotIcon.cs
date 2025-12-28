// Create a new component: UIEquipmentSlotIcon.cs
// This replaces UIEquipmentSlotButton for a more compact icon-only view

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using EldmeresTale.Entities;

namespace EldmeresTale.Core.UI {
	/// <summary>
	/// Compact equipment slot showing only icon and rarity border
	/// </summary>
	public class UIEquipmentSlotIcon : UIElement {
		private BitmapFont _font;
		private Texture2D _pixelTexture;
		private EquipmentSlot _slot;
		private Equipment _equipped;

		public Action OnClick { get; set; }
		public Action<bool, UIElement> OnHover { get; set; }

		private bool _isHovered = false;

		// Slot size - square
		private const int SLOT_SIZE = 32;

		public UIEquipmentSlotIcon(GraphicsDevice graphicsDevice, BitmapFont font,
								  EquipmentSlot slot, Equipment equipped) {
			_font = font;
			_slot = slot;
			_equipped = equipped;

			_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			_pixelTexture.SetData(new[] { Color.White });

			Width = SLOT_SIZE;
			Height = SLOT_SIZE;
		}

		protected override void OnDraw(SpriteBatch spriteBatch) {
			var globalBounds = GlobalBounds;

			// Draw slot background (dark)
			spriteBatch.Draw(_pixelTexture, globalBounds, new Color(20, 20, 20));

			// Draw item icon if equipped
			if(_equipped != null && _equipped.Icon != null) {
				// Draw item icon
				spriteBatch.Draw(_equipped.Icon, globalBounds, Color.White);
			} else {
				// Draw slot type indicator (empty slot)
				DrawSlotPlaceholder(spriteBatch, globalBounds);
			}

			// Draw rarity border if equipped
			if(_equipped != null) {
				Color rarityColor = _equipped.GetRarityColor();
				DrawBorder(spriteBatch, globalBounds, rarityColor, 2);
			} else {
				// Empty slot border (gray)
				DrawBorder(spriteBatch, globalBounds, Color.DarkGray, 1);
			}

			// Highlight on hover
			if(_isHovered) {
				spriteBatch.Draw(_pixelTexture, globalBounds, Color.White * 0.3f);
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

			int textWidth = _font.measureString(symbol);
			int textHeight = _font.getHeight();
			int textX = bounds.X + (bounds.Width - textWidth) / 2;
			int textY = bounds.Y + (bounds.Height - textHeight) / 2;

			_font.drawText(spriteBatch, symbol,
				new Vector2(textX, textY),
				Color.DarkGray * 0.5f);
		}

		protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
			Point mousePos = mouse.Position;
			bool nowHovered = GlobalBounds.Contains(mousePos);

			if(nowHovered != _isHovered) {
				_isHovered = nowHovered;
				OnHover?.Invoke(_isHovered, this);
			}

			// Only clickable if something is equipped (right-click to unequip)
			if(_equipped != null && _isHovered) {
				if(mouse.RightButton == ButtonState.Pressed &&
					previousMouse.RightButton == ButtonState.Released) {
					OnClick?.Invoke();
					return true;
				}
			}

			return _isHovered;
		}

		private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int width) {
			// Top
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
			// Bottom
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
			// Left
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
			// Right
			spriteBatch.Draw(_pixelTexture,
				new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
		}
	}
}