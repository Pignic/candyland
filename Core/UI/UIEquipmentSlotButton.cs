using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

public class UIEquipmentSlotButton : UINavigableElement {

	private readonly EquipmentSlot _slot;
	private readonly string _slotName;
	private readonly Equipment _equipped;
	private readonly int _lineHeight;

	public UIEquipmentSlotButton(EquipmentSlot slot, string slotName, Equipment equipped, int lineHeight) : base() {
		_slot = slot;
		_slotName = slotName;
		_equipped = equipped;
		_lineHeight = lineHeight;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Background
		spriteBatch.Draw(DefaultTexture, globalBounds, new Color(40, 40, 40, 100));

		// Highlight on hover
		if (IsHovered && _equipped != null) {
			spriteBatch.Draw(DefaultTexture, globalBounds, Color.White * 0.2f);
		}

		// Slot label
		Font.DrawText(spriteBatch, _slotName,
			new Vector2(globalBounds.X + 2, globalBounds.Y + 2),
			Color.Cyan);

		// Equipped item
		if (_equipped == null) {
			Font.DrawText(spriteBatch, "  [Empty]",
				new Vector2(globalBounds.X + 2, globalBounds.Y + 2 + _lineHeight),
				Color.DarkGray);
		} else {
			Font.DrawText(spriteBatch, "  " + _equipped.Name,
				new Vector2(globalBounds.X + 2, globalBounds.Y + 2 + _lineHeight),
				_equipped.GetRarityColor());
		}
	}
}