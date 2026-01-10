using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Core.UI;

public class UISlider : UINavigableElement {

	// Slider properties 
	public string Label { get; set; }
	public int MinValue { get; set; }
	public int MaxValue { get; set; }

	private int _value;
	public int Value {
		get => _value;
		set {
			int newValue = MathHelper.Clamp(value, MinValue, MaxValue);
			if (newValue != _value) {
				_value = newValue;
				OnValueChanged?.Invoke(_value);
			}
		}
	}
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

	// State
	private bool _isDragging = false;

	public UISlider(string label, int minValue, int maxValue, int initialValue) : base() {
		Label = label;
		MinValue = minValue;
		MaxValue = maxValue;
		_value = MathHelper.Clamp(initialValue, minValue, maxValue);

		// Set default size
		Width = 200;
		Height = 30;
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Point globalPos = GlobalPosition;

		// Draw label
		if (!string.IsNullOrEmpty(Label)) {
			Font.DrawText(spriteBatch, Label,
				new Vector2(globalPos.X, globalPos.Y), LabelColor);
		}

		// Calculate track position (below label)
		int trackY = globalPos.Y + 15;
		int trackX = globalPos.X;
		int trackWidth = Width;

		// Draw track background
		Rectangle trackRect = new Rectangle(
			trackX,
			trackY - (TRACK_HEIGHT / 2),
			trackWidth,
			TRACK_HEIGHT
		);
		spriteBatch.Draw(DefaultTexture, trackRect, TrackColor);

		// Draw filled portion
		float fillPercent = (float)(Value - MinValue) / (MaxValue - MinValue);
		int fillWidth = (int)(trackWidth * fillPercent);
		Rectangle fillRect = new Rectangle(
			trackX,
			trackY - (TRACK_HEIGHT / 2),
			fillWidth,
			TRACK_HEIGHT
		);
		spriteBatch.Draw(DefaultTexture, fillRect, FillColor);

		// Calculate thumb position
		int thumbX = trackX + fillWidth - (THUMB_SIZE / 2);
		int thumbY = trackY - (THUMB_SIZE / 2);

		// Draw thumb
		Color thumbDrawColor = IsHovered || _isDragging ? Color.Yellow : ThumbColor;
		Rectangle thumbRect = new Rectangle(thumbX, thumbY, THUMB_SIZE, THUMB_SIZE);
		spriteBatch.Draw(DefaultTexture, thumbRect, thumbDrawColor);

		// Draw thumb border
		DrawBorder(spriteBatch, thumbRect, Color.Black, 1);

		// Draw value on the right
		string valueText = Value.ToString();
		Vector2 valuePos = new Vector2(globalPos.X + Width + 10, globalPos.Y);
		Font.DrawText(spriteBatch, valueText, valuePos, ValueColor);
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		UpdateMouseHover(mouse);
		if (!Enabled) {
			_isDragging = false;
			_isMouseHovered = false;
			return false;
		}

		Point mousePos = mouse.Position;
		Point globalPos = GlobalPosition;

		// Calculate track area
		int trackY = globalPos.Y + 15;
		int trackX = globalPos.X;
		int trackWidth = Width;

		// Handle dragging
		if (_isDragging) {
			if (mouse.LeftButton == ButtonState.Pressed) {
				// Update value based on mouse position
				float percent = MathHelper.Clamp(
					(float)(mousePos.X - trackX) / trackWidth,
					0f, 1f
				);
				int newValue = MinValue + (int)((MaxValue - MinValue) * percent);

				// Snap to step
				newValue = MinValue + ((newValue - MinValue) / Step * Step);
				newValue = MathHelper.Clamp(newValue, MinValue, MaxValue);

				if (newValue != Value) {
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
		if (_isMouseHovered &&
		   mouse.LeftButton == ButtonState.Pressed &&
		   previousMouse.LeftButton == ButtonState.Released) {
			_isDragging = true;
			return true;
		}

		return _isMouseHovered;
	}

	private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int width) {
		// Top
		spriteBatch.Draw(DefaultTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
		// Bottom
		spriteBatch.Draw(DefaultTexture, new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
		// Left
		spriteBatch.Draw(DefaultTexture, new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
		// Right
		spriteBatch.Draw(DefaultTexture, new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
	}
}