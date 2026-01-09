using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

public class UIInventorySlot : UINavigableElement {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly Texture2D _pixelTexture;

	private Equipment _item;
	private bool _hasItem;
	private readonly int _slotIndex;

	// Visual state
	private bool _isHovered;
	private const int SLOT_SIZE = 48;
	private const int PADDING = 4;

	public Equipment Item => _item;
	public bool HasItem => _hasItem;
	public int SlotIndex => _slotIndex;

	public event System.Action<UIInventorySlot> OnSlotClicked;
	public event System.Action<UIInventorySlot> OnSlotRightClicked;
	public event System.Action<UIInventorySlot> OnSlotHovered;

	public UIInventorySlot(GraphicsDevice graphicsDevice, BitmapFont font, int slotIndex) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_slotIndex = slotIndex;

		Width = SLOT_SIZE;
		Height = SLOT_SIZE;
		IsNavigable = true;

		// Create pixel texture
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData(new[] { Color.White });

		BackgroundColor = new Color(40, 40, 40);
		BorderColor = Color.Gray;
		BorderWidth = 1;
	}

	/// <summary>
	/// Update slot with new item data (doesn't recreate UI)
	/// </summary>
	public void SetItem(Equipment item) {
		_item = item;
		_hasItem = item != null;
	}

	/// <summary>
	/// Clear the slot
	/// </summary>
	public void Clear() {
		_item = null;
		_hasItem = false;
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		// Slot updates here if needed
	}

	public override bool HandleMouse(Microsoft.Xna.Framework.Input.MouseState mouseState, Microsoft.Xna.Framework.Input.MouseState previousMouseState) {
		Rectangle bounds = GlobalBounds;
		bool wasHovered = _isHovered;
		_isHovered = bounds.Contains(mouseState.Position);

		// Hover events
		if (_isHovered && !wasHovered && _hasItem) {
			OnSlotHovered?.Invoke(this);
		} else if (!_isHovered && wasHovered) {
			// Mouse left slot - clear hover
			OnSlotHovered?.Invoke(this); // Pass this slot but it has no item or is not hovered
		}

		// Click events
		if (_isHovered && _hasItem) {
			// Left click
			if (mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
				previousMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released) {
				OnSlotClicked?.Invoke(this);
				return true;
			}

			// Right click
			if (mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
				previousMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released) {
				OnSlotRightClicked?.Invoke(this);
				return true;
			}
		}

		return _isHovered;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle bounds = GlobalBounds;

		// Draw background
		Color bgColor = _isHovered ? new Color(60, 60, 60) : BackgroundColor;
		spriteBatch.Draw(_pixelTexture, bounds, bgColor);

		// Draw border
		DrawBorder(spriteBatch, bounds);

		// Draw item if present
		if (_hasItem && _item != null) {
			DrawItem(spriteBatch, bounds);
		}
	}

	private void DrawItem(SpriteBatch spriteBatch, Rectangle bounds) {
		// Draw item name (truncated if needed)
		string itemName = _item.Name;
		if (itemName.Length > 10) {
			itemName = itemName.Substring(0, 9) + "...";
		}

		Vector2 textPos = new Vector2(
			bounds.X + PADDING,
			bounds.Y + PADDING
		);

		// Color by rarity
		Color textColor = GetRarityColor(_item.Rarity);
		_font.DrawText(spriteBatch, itemName, textPos, textColor, 1);

		// Draw item type/slot at bottom
		string slotText = _item.Slot.ToString();
		Vector2 slotTextSize = _font.GetSize(slotText, 1);
		Vector2 slotTextPos = new Vector2(
			bounds.X + PADDING,
			bounds.Bottom - PADDING - (int)slotTextSize.Y
		);
		_font.DrawText(spriteBatch, slotText, slotTextPos, Color.Gray, 1);
	}

	private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds) {
		Color borderColor = _isHovered ? Color.Yellow : BorderColor;
		int thickness = BorderWidth;

		// Top
		spriteBatch.Draw(_pixelTexture,
			new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), borderColor);
		// Bottom
		spriteBatch.Draw(_pixelTexture,
			new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), borderColor);
		// Left
		spriteBatch.Draw(_pixelTexture,
			new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), borderColor);
		// Right
		spriteBatch.Draw(_pixelTexture,
			new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), borderColor);
	}

	private static Color GetRarityColor(EquipmentRarity rarity) {
		return rarity switch {
			EquipmentRarity.Common => Color.White,
			EquipmentRarity.Uncommon => Color.LimeGreen,
			EquipmentRarity.Rare => Color.CornflowerBlue,
			EquipmentRarity.Epic => Color.Purple,
			EquipmentRarity.Legendary => Color.Orange,
			_ => Color.White
		};
	}

	public void ForceHoverState(bool hovered) {
		_isHovered = hovered;
	}
}