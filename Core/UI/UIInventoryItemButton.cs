using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIInventoryItemButton : UINavigableElement {
	private readonly Equipment _item;
	private readonly int _lineHeight;

	public UIInventoryItemButton(Equipment item, int lineHeight) : base() {
		_item = item;
		_lineHeight = lineHeight;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Highlight on hover
		if (IsHovered) {
			spriteBatch.Draw(DefaultTexture, globalBounds, Color.White * 0.2f);
		}

		// Item name
		Font.DrawText(spriteBatch, _item.Name,
			new Vector2(globalBounds.X, globalBounds.Y),
			_item.GetRarityColor());

		// Slot type
		Font.DrawText(spriteBatch, $"  [{_item.Slot}]",
			new Vector2(globalBounds.X, globalBounds.Y + _lineHeight),
			Color.LightGray);

		// Quick stats
		string stats = GetItemStatsPreview(_item);
		if (!string.IsNullOrEmpty(stats)) {
			Font.DrawText(spriteBatch, "  " + stats,
				new Vector2(globalBounds.X, globalBounds.Y + (_lineHeight * 2)),
				Color.Gray);
		}
	}

	private static string GetItemStatsPreview(Equipment item) {
		List<string> stats = [];

		if (item.AttackDamageBonus > 0) {
			stats.Add($"+{item.AttackDamageBonus} ATK");
		}

		if (item.DefenseBonus > 0) {
			stats.Add($"+{item.DefenseBonus} DEF");
		}

		if (item.MaxHealthBonus > 0) {
			stats.Add($"+{item.MaxHealthBonus} HP");
		}

		if (item.SpeedBonus > 0) {
			stats.Add($"+{item.SpeedBonus:F0} SPD");
		}

		if (item.CritChanceBonus > 0) {
			stats.Add($"+{item.CritChanceBonus * 100:F0}% CRIT");
		}

		if (stats.Count == 0) {
			return "";
		}

		return string.Join(", ", stats.GetRange(0, Math.Min(2, stats.Count)));
	}
}