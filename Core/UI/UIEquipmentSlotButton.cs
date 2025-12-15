using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Candyland.Entities;

namespace Candyland.Core.UI {
	/// <summary>
	/// Clickable equipment slot with hover support
	/// </summary>
	public class UIEquipmentSlotButton : UIElement {
		private BitmapFont _font;
		private Texture2D _pixelTexture;
		private EquipmentSlot _slot;
		private string _slotName;
		private Equipment _equipped;
		private int _lineHeight;

		public Action OnClick { get; set; }
		public Action<bool> OnHover { get; set; }

		private bool _isHovered = false;

		public UIEquipmentSlotButton(GraphicsDevice graphicsDevice, BitmapFont font,
									EquipmentSlot slot, string slotName, Equipment equipped, int lineHeight) {
			_font = font;
			_slot = slot;
			_slotName = slotName;
			_equipped = equipped;
			_lineHeight = lineHeight;

			_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			_pixelTexture.SetData(new[] { Color.White });
		}

		protected override void OnDraw(SpriteBatch spriteBatch) {
			var globalBounds = GlobalBounds;

			// Background
			spriteBatch.Draw(_pixelTexture, globalBounds, new Color(40, 40, 40, 100));

			// Highlight on hover
			if(_isHovered && _equipped != null) {
				spriteBatch.Draw(_pixelTexture, globalBounds, Color.White * 0.2f);
			}

			// Slot label
			_font.drawText(spriteBatch, _slotName,
				new Vector2(globalBounds.X + 2, globalBounds.Y + 2),
				Color.Cyan);

			// Equipped item
			if(_equipped == null) {
				_font.drawText(spriteBatch, "  [Empty]",
					new Vector2(globalBounds.X + 2, globalBounds.Y + 2 + _lineHeight),
					Color.DarkGray);
			} else {
				_font.drawText(spriteBatch, "  " + _equipped.Name,
					new Vector2(globalBounds.X + 2, globalBounds.Y + 2 + _lineHeight),
					_equipped.GetRarityColor());
			}
		}

		protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
			Point mousePos = mouse.Position;
			bool nowHovered = GlobalBounds.Contains(mousePos);

			if(nowHovered != _isHovered) {
				_isHovered = nowHovered;
				OnHover?.Invoke(_isHovered);
			}

			// Only clickable if something is equipped
			if(_equipped != null && _isHovered &&
				mouse.LeftButton == ButtonState.Pressed &&
				previousMouse.LeftButton == ButtonState.Released) {
				OnClick?.Invoke();
				return true;
			}

			return _isHovered;
		}
	}
}