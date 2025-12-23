using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Candyland.Core.UI;

public class UISlider : UINavigableElement {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly Texture2D _pixelTexture;

	// Slider properties
	public string Label { get; set; }
	public int MinValue { get; set; }
	public int MaxValue { get; set; }
	public int Value { get; set; }
	public int Step { get; set; } = 1;

	// Callback when value changes
	public System.Action<int> OnValueChanged;

	// Visual properties
	public Color TrackColor { get; set; } = new Color(60, 60, 60);
	public Color FillColor { get; set; } = Color.Gold;
	public Color ThumbColor { get; set; } = Color.White;
	public Color LabelColor { get; set; } = Color.White;
	public Color ValueColor { get; set; } = Color.Yellow;

	// Dimensions
	private const int TRACK_HEIGHT = 4;
	private const int THUMB_SIZE = 12;
	private const int LABEL_SPACING = 5;

	// State
	private bool _isDragging = false;
	private bool _isHovered = false;

	public UISlider(GraphicsDevice graphicsDevice, BitmapFont font, string label,
					int minValue, int maxValue, int initialValue) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		Label = label;
		MinValue = minValue;
		MaxValue = maxValue;
		Value = MathHelper.Clamp(initialValue, minValue, maxValue);

		// Create 1x1 white pixel texture
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData(new[] { Color.White });

		// Set default size
		Width = 200;
		Height = 30;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		var globalPos = GlobalPosition;

		// Draw label
		if(!string.IsNullOrEmpty(Label)) {
			_font.drawText(spriteBatch, Label,
				new Vector2(globalPos.X, globalPos.Y), LabelColor);
		}

		// Calculate track position (below label)
		int trackY = globalPos.Y + 15;
		int trackX = globalPos.X;
		int trackWidth = Width;

		// Draw track background
		Rectangle trackRect = new Rectangle(
			trackX,
			trackY - TRACK_HEIGHT / 2,
			trackWidth,
			TRACK_HEIGHT
		);
		spriteBatch.Draw(_pixelTexture, trackRect, TrackColor);

		// Draw filled portion
		float fillPercent = (float)(Value - MinValue) / (MaxValue - MinValue);
		int fillWidth = (int)(trackWidth * fillPercent);
		Rectangle fillRect = new Rectangle(
			trackX,
			trackY - TRACK_HEIGHT / 2,
			fillWidth,
			TRACK_HEIGHT
		);
		spriteBatch.Draw(_pixelTexture, fillRect, FillColor);

		// Calculate thumb position
		int thumbX = trackX + fillWidth - THUMB_SIZE / 2;
		int thumbY = trackY - THUMB_SIZE / 2;

		// Draw thumb
		Color thumbDrawColor = _isHovered || _isDragging ? Color.Yellow : ThumbColor;
		Rectangle thumbRect = new Rectangle(thumbX, thumbY, THUMB_SIZE, THUMB_SIZE);
		spriteBatch.Draw(_pixelTexture, thumbRect, thumbDrawColor);

		// Draw thumb border
		DrawBorder(spriteBatch, thumbRect, Color.Black, 1);

		// Draw value on the right
		string valueText = Value.ToString();
		Vector2 valuePos = new Vector2(globalPos.X + Width + 10, globalPos.Y);
		_font.drawText(spriteBatch, valueText, valuePos, ValueColor);
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		UpdateMouseHover(mouse);
		if(!Enabled) {
			_isDragging = false;
			_isHovered = false;
			return false;
		}

		Point mousePos = mouse.Position;
		var globalPos = GlobalPosition;

		// Calculate track area
		int trackY = globalPos.Y + 15;
		int trackX = globalPos.X;
		int trackWidth = Width;
		Rectangle trackArea = new Rectangle(
			trackX - THUMB_SIZE / 2,
			trackY - THUMB_SIZE,
			trackWidth + THUMB_SIZE,
			THUMB_SIZE * 2
		);

		_isHovered = trackArea.Contains(mousePos);

		// Handle dragging
		if(_isDragging) {
			if(mouse.LeftButton == ButtonState.Pressed) {
				// Update value based on mouse position
				float percent = MathHelper.Clamp(
					(float)(mousePos.X - trackX) / trackWidth,
					0f, 1f
				);
				int newValue = MinValue + (int)((MaxValue - MinValue) * percent);

				// Snap to step
				newValue = MinValue + ((newValue - MinValue) / Step) * Step;
				newValue = MathHelper.Clamp(newValue, MinValue, MaxValue);

				if(newValue != Value) {
					Value = newValue;
					OnValueChanged?.Invoke(Value);
				}
				return true;
			} else {
				// Released - stop dragging
				_isDragging = false;
			}
		}

		// Start dragging
		if(_isHovered &&
		   mouse.LeftButton == ButtonState.Pressed &&
		   previousMouse.LeftButton == ButtonState.Released) {
			_isDragging = true;
			return true;
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