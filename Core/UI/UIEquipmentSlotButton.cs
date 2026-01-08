using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EldmeresTale.Core.UI;

public class UIEquipmentSlotButton : UIElement {

	private readonly BitmapFont _font;
	private readonly Texture2D _pixelTexture;
	private readonly EquipmentSlot _slot;
	private readonly string _slotName;
	private readonly Equipment _equipped;
	private readonly int _lineHeight;

	public Action OnClick { get; set; }
	public Action<bool, UIElement> OnHover { get; set; }

	private bool _isHovered = false;

	public UIEquipmentSlotButton(GraphicsDevice graphicsDevice, BitmapFont font,
								EquipmentSlot slot, string slotName, Equipment equipped, int lineHeight) {
		_font = font;
		_slot = slot;
		_slotName = slotName;
		_equipped = equipped;
		_lineHeight = lineHeight;

		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData([Color.White]);
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Background
		spriteBatch.Draw(_pixelTexture, globalBounds, new Color(40, 40, 40, 100));

		// Highlight on hover
		if (_isHovered && _equipped != null) {
			spriteBatch.Draw(_pixelTexture, globalBounds, Color.White * 0.2f);
		}

		// Slot label
		_font.DrawText(spriteBatch, _slotName,
			new Vector2(globalBounds.X + 2, globalBounds.Y + 2),
			Color.Cyan);

		// Equipped item
		if (_equipped == null) {
			_font.DrawText(spriteBatch, "  [Empty]",
				new Vector2(globalBounds.X + 2, globalBounds.Y + 2 + _lineHeight),
				Color.DarkGray);
		} else {
			_font.DrawText(spriteBatch, "  " + _equipped.Name,
				new Vector2(globalBounds.X + 2, globalBounds.Y + 2 + _lineHeight),
				_equipped.GetRarityColor());
		}
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		Point mousePos = mouse.Position;
		bool nowHovered = GlobalBounds.Contains(mousePos);

		if (nowHovered != _isHovered) {
			_isHovered = nowHovered;
			OnHover?.Invoke(_isHovered, this);
		}

		// Only clickable if something is equipped
		if (_equipped != null && _isHovered &&
			mouse.LeftButton == ButtonState.Pressed &&
			previousMouse.LeftButton == ButtonState.Released) {
			OnClick?.Invoke();
			return true;
		}

		return _isHovered;
	}
}