using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

public class UIEquipmentSlot : UINavigableElement {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly Texture2D _pixelTexture;

	private Equipment _equippedItem;
	private bool _isHovered;

	private const int SLOT_SIZE = 32;

	public EquipmentSlot Slot { get; }
	public Equipment EquippedItem => _equippedItem;
	public bool HasItem => _equippedItem != null;

	public event System.Action OnSlotClicked;
	public event System.Action OnSlotHovered;

	public UIEquipmentSlot(GraphicsDevice graphicsDevice, BitmapFont font, EquipmentSlot slot) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		Slot = slot;

		Width = SLOT_SIZE;
		Height = SLOT_SIZE;
		IsNavigable = true;

		// Create pixel texture
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData([Color.White]);

		BackgroundColor = new Color(50, 50, 50);
		BorderColor = Color.DarkGray;
		BorderWidth = 1;
	}

	public void SetItem(Equipment item) {
		_equippedItem = item;
	}

	public override bool HandleMouse(Microsoft.Xna.Framework.Input.MouseState mouseState, Microsoft.Xna.Framework.Input.MouseState previousMouseState) {
		Rectangle bounds = GlobalBounds;
		bool wasHovered = _isHovered;
		_isHovered = bounds.Contains(mouseState.Position);

		// Hover event
		if (_isHovered && !wasHovered) {
			OnSlotHovered?.Invoke();
		}

		// Click event
		if (_isHovered &&
			mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
			previousMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released) {
			OnSlotClicked?.Invoke();
			return true;
		}

		return _isHovered;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle bounds = GlobalBounds;

		// Background color changes based on state
		Color bgColor = _isHovered ? new Color(70, 70, 70) : BackgroundColor;
		spriteBatch.Draw(_pixelTexture, bounds, bgColor);

		// Draw slot type icon/letter
		DrawSlotIcon(spriteBatch, bounds);

		// Draw equipped item if present
		if (HasItem) {
			DrawEquippedItem(spriteBatch, bounds);
		}

		// Draw border
		DrawBorder(spriteBatch, bounds);
	}

	private void DrawSlotIcon(SpriteBatch spriteBatch, Rectangle bounds) {
		// Get first letter of slot name as icon
		string slotLetter = Slot.ToString().Substring(0, 1);

		Vector2 textSize = _font.GetSize(slotLetter, 1);
		Vector2 textPos = new Vector2(
			bounds.X + (bounds.Width / 2) - (textSize.X / 2),
			bounds.Y + (bounds.Height / 2) - (textSize.Y / 2)
		);

		Color iconColor = HasItem ? new Color(100, 100, 100) : new Color(80, 80, 80);
		_font.DrawText(spriteBatch, slotLetter, textPos, iconColor, 1);
	}

	private void DrawEquippedItem(SpriteBatch spriteBatch, Rectangle bounds) {
		// Draw colored overlay to indicate item is equipped
		Color rarityColor = GetRarityColor(_equippedItem.Rarity);
		rarityColor *= 0.3f; // Semi-transparent overlay

		Rectangle overlayRect = new Rectangle(
			bounds.X + 2,
			bounds.Y + 2,
			bounds.Width - 4,
			bounds.Height - 4
		);
		spriteBatch.Draw(_pixelTexture, overlayRect, rarityColor);

		// Draw small indicator in corner
		Rectangle indicator = new Rectangle(
			bounds.Right - 6,
			bounds.Top + 2,
			4,
			4
		);
		spriteBatch.Draw(_pixelTexture, indicator, GetRarityColor(_equippedItem.Rarity));
	}

	private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds) {
		Color borderColor = _isHovered ? Color.Yellow : (HasItem ? GetRarityColor(_equippedItem.Rarity) : BorderColor);
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
}