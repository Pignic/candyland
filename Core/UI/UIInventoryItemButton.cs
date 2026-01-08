using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIInventoryItemButton : UINavigableElement {
	private readonly BitmapFont _font;
	private readonly Texture2D _pixelTexture;
	private readonly Equipment _item;
	private readonly int _lineHeight;

	public Action OnClick { get; set; }
	public Action<bool, UIElement> OnHover { get; set; }

	public UIInventoryItemButton(GraphicsDevice graphicsDevice, BitmapFont font, Equipment item, int lineHeight) {
		_font = font;
		_item = item;
		_lineHeight = lineHeight;

		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData([Color.White]);
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Rectangle globalBounds = GlobalBounds;

		// Highlight on hover
		if (IsHovered) {
			spriteBatch.Draw(_pixelTexture, globalBounds, Color.White * 0.2f);
		}

		// Item name
		_font.DrawText(spriteBatch, _item.Name,
			new Vector2(globalBounds.X, globalBounds.Y),
			_item.GetRarityColor());

		// Slot type
		_font.DrawText(spriteBatch, $"  [{_item.Slot}]",
			new Vector2(globalBounds.X, globalBounds.Y + _lineHeight),
			Color.LightGray);

		// Quick stats
		string stats = GetItemStatsPreview(_item);
		if (!string.IsNullOrEmpty(stats)) {
			_font.DrawText(spriteBatch, "  " + stats,
				new Vector2(globalBounds.X, globalBounds.Y + (_lineHeight * 2)),
				Color.Gray);
		}
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		Point mousePos = mouse.Position;
		bool nowHovered = GlobalBounds.Contains(mousePos);

		if (nowHovered != IsHovered) {
			_isMouseHovered = nowHovered;
			OnHover?.Invoke(IsHovered, this);
		}

		if (IsHovered && mouse.LeftButton == ButtonState.Pressed &&
			previousMouse.LeftButton == ButtonState.Released) {
			OnClick?.Invoke();
			return true;
		}

		return IsHovered;
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