using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Candyland.Entities;

namespace Candyland.Core.UI {

	public class UIInventoryItemButton : UINavigableElement {
		private BitmapFont _font;
		private Texture2D _pixelTexture;
		private Equipment _item;
		private int _lineHeight;

		public Action OnClick { get; set; }
		public Action<bool, UIElement> OnHover { get; set; }

		public UIInventoryItemButton(GraphicsDevice graphicsDevice, BitmapFont font, Equipment item, int lineHeight) {
			_font = font;
			_item = item;
			_lineHeight = lineHeight;

			_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			_pixelTexture.SetData(new[] { Color.White });
		}

		protected override void OnDraw(SpriteBatch spriteBatch) {
			var globalBounds = GlobalBounds;

			// Highlight on hover
			if(IsHovered) {
				spriteBatch.Draw(_pixelTexture, globalBounds, Color.White * 0.2f);
			}

			// Item name
			_font.drawText(spriteBatch, _item.Name,
				new Vector2(globalBounds.X, globalBounds.Y),
				_item.GetRarityColor());

			// Slot type
			_font.drawText(spriteBatch, $"  [{_item.Slot}]",
				new Vector2(globalBounds.X, globalBounds.Y + _lineHeight),
				Color.LightGray);

			// Quick stats
			string stats = GetItemStatsPreview(_item);
			if(!string.IsNullOrEmpty(stats)) {
				_font.drawText(spriteBatch, "  " + stats,
					new Vector2(globalBounds.X, globalBounds.Y + _lineHeight * 2),
					Color.Gray);
			}
		}

		protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
			Point mousePos = mouse.Position;
			bool nowHovered = GlobalBounds.Contains(mousePos);

			if(nowHovered != IsHovered) {
				_isMouseHovered = nowHovered;
				OnHover?.Invoke(IsHovered, this);
			}

			if(IsHovered && mouse.LeftButton == ButtonState.Pressed &&
				previousMouse.LeftButton == ButtonState.Released) {
				OnClick?.Invoke();
				return true;
			}

			return IsHovered;
		}

		private string GetItemStatsPreview(Equipment item) {
			var stats = new System.Collections.Generic.List<string>();

			if(item.AttackDamageBonus > 0) stats.Add($"+{item.AttackDamageBonus} ATK");
			if(item.DefenseBonus > 0) stats.Add($"+{item.DefenseBonus} DEF");
			if(item.MaxHealthBonus > 0) stats.Add($"+{item.MaxHealthBonus} HP");
			if(item.SpeedBonus > 0) stats.Add($"+{item.SpeedBonus:F0} SPD");
			if(item.CritChanceBonus > 0) stats.Add($"+{item.CritChanceBonus * 100:F0}% CRIT");

			if(stats.Count == 0) return "";

			return string.Join(", ", stats.GetRange(0, Math.Min(2, stats.Count)));
		}
	}
}