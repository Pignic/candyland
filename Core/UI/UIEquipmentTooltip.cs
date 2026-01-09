using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIEquipmentTooltip : UIComponent {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly Texture2D _pixelTexture;

	private Equipment _item;
	private Rectangle _bounds;

	// Cached tooltip data (rebuilt when item changes)
	private readonly List<string> _lines;
	private int _tooltipWidth;
	private int _tooltipHeight;

	public Equipment Item {
		get => _item;
		set {
			if (_item != value) {
				_item = value;
				RebuildTooltip();
			}
		}
	}

	public Rectangle Bounds => _bounds;

	public UIEquipmentTooltip(GraphicsDevice graphicsDevice, BitmapFont font) : base(font, 0, 0) {
		_graphicsDevice = graphicsDevice;

		// Create pixel texture
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData(new[] { Color.White });

		_lines = [];
	}

	private void RebuildTooltip() {
		_lines.Clear();

		if (_item == null) {
			_tooltipWidth = 0;
			_tooltipHeight = 0;
			return;
		}

		// Build tooltip lines
		_lines.Add(_item.Name);
		_lines.Add($"[{_item.Rarity}]");
		_lines.Add(_item.Slot.ToString());

		if (_item.RequiredLevel > 1) {
			_lines.Add($"Requires Level {_item.RequiredLevel}");
		}

		_lines.Add("");

		if (!string.IsNullOrEmpty(_item.Description)) {
			_lines.Add(_item.Description);
			_lines.Add("");
		}

		// Add stats (only non-zero values)
		if (_item.MaxHealthBonus != 0) {
			_lines.Add($"+{_item.MaxHealthBonus} Max Health");
		}

		if (_item.AttackDamageBonus != 0) {
			_lines.Add($"+{_item.AttackDamageBonus} Attack Damage");
		}

		if (_item.DefenseBonus != 0) {
			_lines.Add($"+{_item.DefenseBonus} Defense");
		}

		if (_item.SpeedBonus != 0) {
			_lines.Add($"+{_item.SpeedBonus:F0} Speed");
		}

		if (_item.AttackSpeedBonus != 0) {
			_lines.Add($"+{_item.AttackSpeedBonus:F2} Attack Speed");
		}

		if (_item.CritChanceBonus != 0) {
			_lines.Add($"+{_item.CritChanceBonus * 100:F0}% Crit Chance");
		}

		if (_item.CritMultiplierBonus != 0) {
			_lines.Add($"+{_item.CritMultiplierBonus:F2}x Crit Damage");
		}

		if (_item.HealthRegenBonus != 0) {
			_lines.Add($"+{_item.HealthRegenBonus:F1} HP Regen");
		}

		if (_item.LifeStealBonus != 0) {
			_lines.Add($"+{_item.LifeStealBonus * 100:F0}% Life Steal");
		}

		if (_item.DodgeChanceBonus != 0) {
			_lines.Add($"+{_item.DodgeChanceBonus * 100:F0}% Dodge");
		}

		// Calculate tooltip size
		int lineHeight = Font.GetHeight(2);
		_tooltipWidth = 0;
		foreach (string line in _lines) {
			int lineWidth = Font.MeasureString(line);
			if (lineWidth > _tooltipWidth) {
				_tooltipWidth = lineWidth;
			}
		}
		_tooltipWidth += 20; // Padding
		_tooltipHeight = (_lines.Count * lineHeight) + 10; // Padding
	}

	public void UpdatePosition(Point position, Rectangle clampBounds) {
		X = position.X;
		Y = position.Y;

		// Clamp X (keep inside bounds horizontally)
		int x = X;
		if (x + _tooltipWidth > clampBounds.Right) {
			x = clampBounds.Right - _tooltipWidth;
		}
		if (x < clampBounds.Left) {
			x = clampBounds.Left;
		}

		// Clamp Y (keep inside bounds vertically)
		int y = Y;
		if (y + _tooltipHeight > clampBounds.Bottom) {
			y = clampBounds.Bottom - _tooltipHeight;
		}
		if (y < clampBounds.Top) {
			y = clampBounds.Top;
		}

		_bounds = new Rectangle(x, y, _tooltipWidth, _tooltipHeight);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if (_item == null || _lines.Count == 0) {
			return;
		}

		// Draw background
		spriteBatch.Draw(_pixelTexture, _bounds, new Color(20, 20, 20, 240));

		// Draw border
		DrawBorder(spriteBatch, _bounds, Color.White, 2);

		// Draw title border (under name)
		int lineHeight = Font.GetHeight(2);
		Rectangle titleBorder = new Rectangle(
			_bounds.X + 5,
			_bounds.Y + (lineHeight * 3) + 2,
			_bounds.Width - 10,
			1
		);
		spriteBatch.Draw(_pixelTexture, titleBorder, Color.Gray);

		// Draw text lines
		int currentY = _bounds.Y + 5;
		for (int i = 0; i < _lines.Count; i++) {
			string line = _lines[i];
			if (string.IsNullOrEmpty(line)) {
				// Empty line (spacer)
				currentY += lineHeight / 2;
				continue;
			}

			Color textColor = GetLineColor(i);
			Font.DrawText(
				spriteBatch,
				line,
				new Vector2(_bounds.X + 10, currentY),
				textColor,
				2
			);

			currentY += lineHeight;
		}
	}

	private Color GetLineColor(int lineIndex) {
		if (lineIndex == 0) {
			// Item name - use rarity color
			return _item.GetRarityColor();
		} else if (lineIndex == 1) {
			// Rarity text - gray
			return Color.Gray;
		} else if (lineIndex == 2) {
			// Slot type - light gray
			return Color.LightGray;
		} else if (_lines[lineIndex].StartsWith("Requires")) {
			// Level requirement - yellow
			return Color.Yellow;
		} else if (_lines[lineIndex].StartsWith("+")) {
			// Stat bonus - green
			return Color.LimeGreen;
		} else {
			// Description - white
			return Color.White;
		}
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

	public void Clear() {
		_item = null;
		_lines.Clear();
		_tooltipWidth = 0;
		_tooltipHeight = 0;
	}
}