using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Candyland.Core.UI;

public class UICheckbox : UINavigableElement {

	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly Texture2D _pixelTexture;

	// Checkbox properties
	public string Label { get; set; }
	public bool IsChecked { get; set; }

	// Callback when value changes
	public System.Action<bool> OnValueChanged;

	// Visual properties
	public Color BoxColor { get; set; } = new Color(60, 60, 60);
	public Color CheckColor { get; set; } = Color.Gold;
	public Color HoverColor { get; set; } = new Color(100, 100, 100);
	public Color LabelColor { get; set; } = Color.White;

	// Dimensions
	private const int BOX_SIZE = 16;
	private const int LABEL_SPACING = 8;

	// State
	private bool _isHovered = false;
	private bool _wasPressed = false;

	public UICheckbox(GraphicsDevice graphicsDevice, BitmapFont font, string label, bool initialValue = false) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		Label = label;
		IsChecked = initialValue;

		// Create 1x1 white pixel texture
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData(new[] { Color.White });

		// Set default size
		Width = BOX_SIZE + LABEL_SPACING + (string.IsNullOrEmpty(label) ? 0 : 100);
		Height = BOX_SIZE;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		var globalPos = GlobalPosition;

		// Draw checkbox box
		Rectangle boxRect = new Rectangle(globalPos.X, globalPos.Y, BOX_SIZE, BOX_SIZE);
		Color bgColor = _isHovered ? HoverColor : BoxColor;
		spriteBatch.Draw(_pixelTexture, boxRect, bgColor);

		// Draw border
		DrawBorder(spriteBatch, boxRect, Color.White, 2);

		// Draw checkmark if checked
		if(IsChecked) {
			// Draw an "X" checkmark
			int padding = 3;
			Rectangle checkRect = new Rectangle(
				globalPos.X + padding,
				globalPos.Y + padding,
				BOX_SIZE - padding * 2,
				BOX_SIZE - padding * 2
			);
			spriteBatch.Draw(_pixelTexture, checkRect, CheckColor);
		}

		// Draw label
		if(!string.IsNullOrEmpty(Label)) {
			Vector2 labelPos = new Vector2(
				globalPos.X + BOX_SIZE + LABEL_SPACING,
				globalPos.Y + (BOX_SIZE - 8) / 2  // Center vertically (assuming 8px font height)
			);
			_font.drawText(spriteBatch, Label, labelPos, LabelColor);
		}
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		UpdateMouseHover(mouse);
		if(!Enabled) {
			_isHovered = false;
			_wasPressed = false;
			return false;
		}

		Point mousePos = mouse.Position;
		var globalPos = GlobalPosition;

		// Expand clickable area to include label
		Rectangle clickArea = new Rectangle(globalPos.X, globalPos.Y, Width, Height);
		_isHovered = clickArea.Contains(mousePos);

		if(_isHovered) {
			// Track press
			if(mouse.LeftButton == ButtonState.Pressed &&
			   previousMouse.LeftButton == ButtonState.Released) {
				_wasPressed = true;
			}

			// Toggle on release (complete click)
			if(mouse.LeftButton == ButtonState.Released &&
			   previousMouse.LeftButton == ButtonState.Pressed &&
			   _wasPressed) {
				IsChecked = !IsChecked;
				OnValueChanged?.Invoke(IsChecked);
				_wasPressed = false;
				return true;
			}
		} else {
			_wasPressed = false;
		}

		return _isHovered;
	}

	private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int width) {
		// Top
		spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
		// Bottom
		spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
		// Left
		spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
		// Right
		spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
	}
}