using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Entities;

/// <summary>
/// Animated sword swing arc with alternating directions
/// </summary>
public class AttackEffect {
	public Vector2 Position { get; set; }
	public bool IsActive { get; private set; }

	private Texture2D _pixelTexture;
	private float _duration = 0.2f;
	private float _timer = 0f;
	private Vector2 _direction;
	private bool _isClockwise;
	private static bool _lastWasClockwise = false;  // Track for alternating

	// Arc properties
	private float arcRadius;           // Distance from player
	private float arcWidth = 20f;             // Thickness of arc
	private const float ARC_SPREAD = MathHelper.PiOver2;  // 90 degrees
	private const int ARC_SEGMENTS = 12;            // Smoothness

	public AttackEffect(GraphicsDevice graphicsDevice) {
		// Create 1x1 white pixel for drawing
		_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
		_pixelTexture.SetData(new[] { Color.White });
		IsActive = false;
	}

	public void Trigger(Vector2 position, Vector2 direction, float range) {
		Position = position;
		_direction = direction;
		IsActive = true;
		_timer = 0f;
		arcRadius = range;
		arcWidth = range;

		// Alternate swing direction
		_isClockwise = !_lastWasClockwise;
		_lastWasClockwise = _isClockwise;

		System.Diagnostics.Debug.WriteLine($"[ATTACK] Swing triggered - Direction: {(_isClockwise ? "Clockwise" : "Counter-clockwise")}");
	}

	public void Update(GameTime gameTime) {
		if(!IsActive) return;

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		_timer += deltaTime;

		if(_timer >= _duration) {
			IsActive = false;
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		if(!IsActive) return;

		// Calculate progress (0 to 1)
		float progress = _timer / _duration;

		// Ease out for natural swing feel
		float easedProgress = 1f - (float)Math.Pow(1f - progress, 3);  // Cubic ease-out

		// Calculate base angle from direction
		float baseAngle = (float)Math.Atan2(_direction.Y, _direction.X);

		// Calculate current sweep angle based on progress
		float sweepAngle = ARC_SPREAD * easedProgress;

		// Start angle (45 degrees to one side)
		float startAngle = _isClockwise 
			? baseAngle - (ARC_SPREAD / 2)   // Start left, sweep right
			: baseAngle + (ARC_SPREAD / 2);  // Start right, sweep left

		// End angle (current position in sweep)
		float endAngle = _isClockwise
			? startAngle + sweepAngle
			: startAngle - sweepAngle;

		// Calculate alpha (fade out at end)
		float alpha = progress < 0.7f ? 1f : 1f - ((progress - 0.7f) / 0.3f);

		// Draw the arc as segments
		DrawArc(spriteBatch, Position, startAngle, endAngle, alpha);
	}

	private void DrawArc(SpriteBatch spriteBatch, Vector2 center, float startAngle, float endAngle, float alpha) {
		// Draw arc as connected line segments
		int segments = (int)(ARC_SEGMENTS * Math.Abs(endAngle - startAngle) / ARC_SPREAD);
		if(segments < 3) segments = 3;

		float angleStep = (endAngle - startAngle) / segments;

		for(int i = 0; i < segments; i++) {
			float angle1 = startAngle + angleStep * i;
			float angle2 = startAngle + angleStep * (i + 1);

			// Calculate segment fade (brighter at tip)
			float segmentAlpha = alpha * (0.5f + 0.5f * (i / (float)segments));

			// Inner arc points
			Vector2 p1Inner = center + new Vector2(
				(float)Math.Cos(angle1) * (arcRadius - arcWidth / 2),
				(float)Math.Sin(angle1) * (arcRadius - arcWidth / 2)
			);

			Vector2 p2Inner = center + new Vector2(
				(float)Math.Cos(angle2) * (arcRadius - arcWidth / 2),
				(float)Math.Sin(angle2) * (arcRadius - arcWidth / 2)
			);

			// Outer arc points
			Vector2 p1Outer = center + new Vector2(
				(float)Math.Cos(angle1) * (arcRadius + arcWidth / 2),
				(float)Math.Sin(angle1) * (arcRadius + arcWidth / 2)
			);

			Vector2 p2Outer = center + new Vector2(
				(float)Math.Cos(angle2) * (arcRadius + arcWidth / 2),
				(float)Math.Sin(angle2) * (arcRadius + arcWidth / 2)
			);

			// Color gradient (white to cyan for energy feel)
			Color baseColor = Color.Lerp(Color.White, Color.Cyan, i / (float)segments);
			Color color = baseColor * segmentAlpha;

			// Draw quad (two triangles) for this segment
			DrawQuad(spriteBatch, p1Inner, p2Inner, p2Outer, p1Outer, color);
		}

		// Add bright trail particles at the tip for extra juice!
		DrawTrailEffect(spriteBatch, center, endAngle, alpha);
	}

	private void DrawQuad(SpriteBatch spriteBatch, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Color color) {
		// Draw as two triangles using line segments (approximate)
		// This is a simple approach - for perfect quads you'd need VertexBuffer
		
		// Draw thick lines to approximate filled quad
		DrawThickLine(spriteBatch, p1, p2, color, 2f);
		DrawThickLine(spriteBatch, p2, p3, color, 2f);
		DrawThickLine(spriteBatch, p3, p4, color, 2f);
		DrawThickLine(spriteBatch, p4, p1, color, 2f);
		
		// Fill interior with more lines
		DrawThickLine(spriteBatch, p1, p3, color, 1f);
		DrawThickLine(spriteBatch, p2, p4, color, 1f);
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

	private void DrawTrailEffect(SpriteBatch spriteBatch, Vector2 center, float angle, float alpha) {
		// Draw a few bright particles at the tip of the swing for extra impact
		for(int i = 0; i < 3; i++) {
			float offset = (i - 1) * 3f;  // Spread particles slightly
			Vector2 tipPos = center + new Vector2(
				(float)Math.Cos(angle) * (arcRadius + offset),
				(float)Math.Sin(angle) * (arcRadius + offset)
			);

			float particleAlpha = alpha * (1f - i * 0.3f);  // Fade back particles
			Color particleColor = Color.Yellow * particleAlpha;

			// Draw small bright particle
			Rectangle particleRect = new Rectangle(
				(int)tipPos.X - 2,
				(int)tipPos.Y - 2,
				4,
				4
			);

			spriteBatch.Draw(_pixelTexture, particleRect, particleColor);
		}
	}

	public void Dispose() {
		_pixelTexture?.Dispose();
	}
}