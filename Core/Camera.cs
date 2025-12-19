using Microsoft.Xna.Framework;
using System;

namespace Candyland.Core;

public class Camera {
	public Vector2 Position { get; set; }
	public float Zoom { get; set; }
	public int ViewportWidth { get; private set; }
	public int ViewportHeight { get; private set; }

	// Bounds for clamping camera to world
	public Rectangle? WorldBounds { get; set; }

	// The transformation matrix used by SpriteBatch
	public Matrix Transform { get; private set; }

	// Center of the camera viewport
	public Vector2 Center => new Vector2(ViewportWidth / 2f, ViewportHeight / 2f);

	// Smoothing parameters
	private const float SMOOTHING_SPEED = 8f;  // Higher = faster catchup
	private const float DEADZONE_THRESHOLD = 0.5f;  // Stop moving when this close

	public Camera(int viewportWidth, int viewportHeight) {
		ViewportWidth = viewportWidth;
		ViewportHeight = viewportHeight;
		Zoom = 1f;
		Position = Vector2.Zero;
	}

	public void Update() {
		// Clamp camera position to world bounds if set
		if(WorldBounds.HasValue) {
			var bounds = WorldBounds.Value;

			// Calculate camera boundaries
			float minX = ViewportWidth / 2f / Zoom;
			float maxX = bounds.Width - (ViewportWidth / 2f / Zoom);
			float minY = ViewportHeight / 2f / Zoom;
			float maxY = bounds.Height - (ViewportHeight / 2f / Zoom);

			Position = new Vector2(
				MathHelper.Clamp(Position.X, minX, maxX),
				MathHelper.Clamp(Position.Y, minY, maxY)
			);
		}

		// Build the transformation matrix
		Transform =
			Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
			Matrix.CreateScale(Zoom) *
			Matrix.CreateTranslation(new Vector3(ViewportWidth / 2f, ViewportHeight / 2f, 0));
	}

	// Follow a target (like the player)
	public void Follow(Vector2 targetPosition) {
		Position = targetPosition;
	}

	public void FollowSmooth(Vector2 targetPosition, float deltaTime) {
		// Calculate distance to target
		Vector2 diff = targetPosition - Position;
		float distance = diff.Length();

		// Use deadzone to prevent jitter when very close
		if(distance < DEADZONE_THRESHOLD) {
			Position = targetPosition;  // Snap when very close
			return;
		}

		// Exponential smoothing (frame-rate independent)
		float smoothFactor = 1f - (float)Math.Pow(0.5f, SMOOTHING_SPEED * deltaTime);
		Position = Vector2.Lerp(Position, targetPosition, smoothFactor);
		Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));
	}

	// Convert screen coordinates to world coordinates
	public Vector2 ScreenToWorld(Vector2 screenPosition) {
		return Vector2.Transform(screenPosition, Matrix.Invert(Transform));
	}

	// Convert world coordinates to screen coordinates
	public Vector2 WorldToScreen(Vector2 worldPosition) {
		return Vector2.Transform(worldPosition, Transform);
	}

	// Get the visible area of the world
	public Rectangle GetVisibleArea() {
		var inverseTransform = Matrix.Invert(Transform);
		var topLeft = Vector2.Transform(Vector2.Zero, inverseTransform);
		var bottomRight = Vector2.Transform(
			new Vector2(ViewportWidth, ViewportHeight),
			inverseTransform
		);

		return new Rectangle(
			(int)topLeft.X,
			(int)topLeft.Y,
			(int)(bottomRight.X - topLeft.X),
			(int)(bottomRight.Y - topLeft.Y)
		);
	}
}