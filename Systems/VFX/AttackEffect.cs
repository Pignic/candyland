using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Systems.VFX;

public class AttackEffect {
	private Func<Vector2> _position;
	public bool IsActive { get; private set; }

	private readonly Texture2D _pixelTexture;
	private readonly float _duration = 0.15f;
	private float _timer = 0f;
	private Vector2 _direction;
	private bool _isClockwise;
	private static bool _lastWasClockwise = false;  // Track for alternating

	// Arc properties
	private float _arcRadius;           // Distance from player
	private float _arcAngle;
	private const int ARC_SEGMENTS = 15;            // Smoothness

	public AttackEffect(GraphicsDevice graphicsDevice) {
		// Create 1x1 white pixel for drawing
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData([Color.White]);
		IsActive = false;
	}

	public void Trigger(Func<Vector2> getPosition, Vector2 direction, float range, float arcAngle = MathHelper.PiOver2) {
		_position = getPosition;
		_direction = direction;
		IsActive = true;
		_timer = 0f;
		_arcRadius = range - 3;
		_arcAngle = arcAngle;

		// Alternate swing direction
		_isClockwise = !_lastWasClockwise;
		_lastWasClockwise = _isClockwise;

		System.Diagnostics.Debug.WriteLine($"[ATTACK] Swing triggered - Direction: {(_isClockwise ? "Clockwise" : "Counter-clockwise")}");
	}

	public void Update(GameTime gameTime) {
		if (!IsActive) {
			return;
		}

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		_timer += deltaTime;

		if (_timer >= _duration) {
			IsActive = false;
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (!IsActive) {
			return;
		}

		// Calculate progress (0 to 1)
		float progress = _timer / _duration;

		// Ease out for natural swing feel
		float easedProgress = 1f - (float)Math.Pow(1f - progress, 3);  // Cubic ease-out

		// Calculate base angle from direction
		float baseAngle = (float)Math.Atan2(_direction.Y, _direction.X);

		// Calculate current sweep angle based on progress
		float sweepAngle = _arcAngle * easedProgress;

		// Start angle
		float startAngle = _isClockwise
			? baseAngle - (_arcAngle / 2)   // Start left, sweep right
			: baseAngle + (_arcAngle / 2);  // Start right, sweep left

		// End angle
		float endAngle = _isClockwise
			? startAngle + sweepAngle
			: startAngle - sweepAngle;

		// Calculate alpha (fade out at end)
		float alpha = progress < 0.7f ? 1f : 1f - ((progress - 0.7f) / 0.3f);

		// Draw the arc as segments
		DrawArc(spriteBatch, _position(), startAngle, endAngle, alpha);
	}

	private void DrawArc(SpriteBatch spriteBatch, Vector2 center, float startAngle, float endAngle, float alpha) {
		// Draw 3 offset arcs for motion blur
		for (int layer = 0; layer < 3; layer++) {
			float layerOffset = -layer * 0.05f;  // Trail behind swing
			float layerAlpha = alpha * (1f - (layer * 0.3f));  // Fade back layers

			float layerStartAngle = startAngle + layerOffset;
			float layerEndAngle = endAngle + layerOffset;

			DrawSingleArc(spriteBatch, center, layerStartAngle, layerEndAngle, layerAlpha, layer);
		}
	}

	private void DrawSingleArc(SpriteBatch spriteBatch, Vector2 center, float startAngle, float endAngle,
		float alpha, int layer) {

		float angleStep = (endAngle - startAngle) / ARC_SEGMENTS;

		for (int i = 0; i <= ARC_SEGMENTS; i++) {
			float angle1 = startAngle + (angleStep * i);
			float angle2 = startAngle + (angleStep * (i + 1));

			Vector2 p1 = center + new Vector2(
				(float)Math.Cos(angle1) * 10,
				(float)Math.Sin(angle1) * 10
			);

			Vector2 p2 = center + new Vector2(
				(float)Math.Cos(angle1) * _arcRadius,
				(float)Math.Sin(angle1) * _arcRadius
			);

			Vector2 p3 = center + new Vector2(
				(float)Math.Cos(angle2) * _arcRadius,
				(float)Math.Sin(angle2) * _arcRadius
			);

			// Color gets whiter toward tip
			float tipness = i / (float)ARC_SEGMENTS;
			Color color = Color.Lerp(Color.LightBlue, Color.White, tipness) * alpha;

			DrawThickLine(spriteBatch, p1, p2, color, 3);
		}
	}

	private void DrawThickLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness) {
		Vector2 edge = end - start;
		float angle = (float)Math.Atan2(edge.Y, edge.X);
		float length = edge.Length();

		spriteBatch.Draw(
			_pixelTexture,
			start,
			null,
			color,
			angle,
			new Vector2(0, 0.5f),
			new Vector2(length, thickness),
			SpriteEffects.None,
			0f
		);
	}

	public void Dispose() {
		_pixelTexture?.Dispose();
	}
}