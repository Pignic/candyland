using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Candyland.Core.UI;

public class UIButton : UINavigableElement {
	private BitmapFont _font;
	private Texture2D _pixelTexture;
	private bool _forceHover = false;

	public string Text { get; set; }
	public Action OnClick { get; set; }

	public void Click() {
		OnClick?.Invoke();
	}

	// Styling
	public Color HoverColor { get; set; } = new Color(80, 80, 80);
	public Color HoverTextColor { get; set; } = new Color(180, 180, 0);
	public Color PressedColor { get; set; } = new Color(40, 40, 40);
	public Color TextColor { get; set; } = Color.White;

	// State
	public bool _isHovered => _forceHover || _isMouseHovered;
	private bool _isMouseHovered = false;
	private bool _isPressed = false;
	private bool _waitingForMouseRelease = false;

	// === FIX: Properly override Enabled to sync with base ===
	public new bool Enabled {
		get => base.Enabled;
		set {
			bool wasEnabled = base.Enabled;
			base.Enabled = value;

			// Detect transition from disabled to enabled
			if(!wasEnabled && value) {
				_waitingForMouseRelease = true;
			}

			// Clear states when disabled
			if(!value) {
				_isMouseHovered = false;
				_forceHover = false;
				_isPressed = false;
				_waitingForMouseRelease = false;
			}
		}
	}

	public enum TextAlignment {
		Left,
		Center,
		Right
	}
	public TextAlignment Alignment { get; set; } = TextAlignment.Center;
	public int TextPadding { get; set; } = 5;

	public UIButton(GraphicsDevice graphicsDevice, BitmapFont font, string text) {
		_font = font;
		Text = text;

		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData(new[] { Color.White });

		// Default size based on text
		int textWidth = font.measureString(text);
		int textHeight = font.getHeight();
		Width = textWidth + 20;
		Height = textHeight + 10;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		var globalBounds = GlobalBounds;

		// Determine background color based on state
		Color bgColor = BackgroundColor;
		if(!Enabled)
			bgColor = BackgroundColor * 0.5f;
		else if(_isPressed)
			bgColor = PressedColor;
		else if(_isHovered)
			bgColor = HoverColor;

		// Background
		spriteBatch.Draw(_pixelTexture, globalBounds, bgColor);

		// Border
		if(BorderWidth > 0) {
			DrawBorder(spriteBatch, globalBounds, BorderColor, BorderWidth);
		}


		// Text with alignment
		if(!string.IsNullOrEmpty(Text)) {
			int textWidth = _font.measureString(Text);
			int textHeight = _font.getHeight();
			int textX;
			int textY = globalBounds.Y + (globalBounds.Height - textHeight) / 2;

			switch(Alignment) {
				case TextAlignment.Left:
					textX = globalBounds.X + TextPadding;
					break;
				case TextAlignment.Right:
					textX = globalBounds.X + globalBounds.Width - textWidth - TextPadding;
					break;
				case TextAlignment.Center:
				default:
					textX = globalBounds.X + (globalBounds.Width - textWidth) / 2;
					break;
			}

			_font.drawText(spriteBatch, Text, new Vector2(textX, textY),
				_isHovered ? HoverTextColor : TextColor);
		}
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		UpdateMouseHover(mouse);
		if(!Enabled) {
			_isMouseHovered = false;
			_forceHover = false;
			_isPressed = false;
			_waitingForMouseRelease = false;
			return false;
		}

		// Wait for mouse release after becoming enabled
		if(_waitingForMouseRelease) {
			if(mouse.LeftButton == ButtonState.Released) {
				_waitingForMouseRelease = false;
			} else {
				// Mouse still pressed - don't allow hover or click
				_isMouseHovered = false;
				_forceHover = false;
				_isPressed = false;
				return false;
			}
		}

		Point mousePos = mouse.Position;
		_isMouseHovered = Enabled && GlobalBounds.Contains(mousePos) && Visible;

		// Check for click
		if(_isHovered) {
			// Pressed
			if(mouse.LeftButton == ButtonState.Pressed &&
					previousMouse.LeftButton == ButtonState.Released) {
				_isPressed = true;
			}

			// Released (click complete)
			if(mouse.LeftButton == ButtonState.Released &&
				previousMouse.LeftButton == ButtonState.Pressed &&
				_isPressed) {
				_isPressed = false;
				OnClick?.Invoke();
				return true;
			}
		} else {
			_isPressed = false;
		}

		return _isHovered;
	}
	public void ForceHoverState(bool hovered) {
		_forceHover = hovered;
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
}