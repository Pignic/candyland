using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Core;

public class Camera {
	public Vector2 Position {
		get => _basePosition + _shakeOffset;
		set => _basePosition = value;
	}
	public float Zoom { get; set; }
	public int ViewportWidth { get; private set; }
	public int ViewportHeight { get; private set; }

	// Bounds for clamping camera to world
	public Rectangle? WorldBounds { get; set; }

	// The transformation matrix used by SpriteBatch
	public Matrix Transform { get; private set; }

	// Center of the camera viewport
	public Vector2 Center => new Vector2(ViewportWidth / 2f, ViewportHeight / 2f);

	private Vector2 _basePosition;  // Position before shake
	private float _shakeIntensity = 0f;
	private float _shakeDuration = 0f;
	private float _shakeTimer = 0f;
	private Vector2 _shakeOffset = Vector2.Zero;
	private float _lastUpdateTime = 0f;
	private readonly Random _shakeRandom = new Random();

	public void SetSize(int width, int height) {
		ViewportWidth = width;
		ViewportHeight = height;
	}

	// Smoothing parameters
	private const float SMOOTHING_SPEED = 8f;  // Higher = faster catchup
	private const float DEADZONE_THRESHOLD = 0.5f;  // Stop moving when this close

	public Camera(int viewportWidth, int viewportHeight) {
		ViewportWidth = viewportWidth;
		ViewportHeight = viewportHeight;
		Zoom = 1f;
		Position = Vector2.Zero;
		_lastUpdateTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;
		Update();
	}
	public void Shake(float intensity, float duration, bool overrideSettings = false) {
		if (GameSettings.Instance.CameraShake || overrideSettings) {
			_shakeIntensity = intensity;
			_shakeDuration = duration;
			_shakeTimer = 0f;
		}
	}

	public void Update() {
		// Update shake effect
		float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;
		float deltaTime = currentTime - _lastUpdateTime;
		_lastUpdateTime = currentTime;

		if (_shakeTimer < _shakeDuration) {
			_shakeTimer += deltaTime;
			float progress = _shakeTimer / _shakeDuration;
			float currentIntensity = _shakeIntensity * (1f - progress);  // Decay

			_shakeOffset = new Vector2(
				((_shakeRandom.NextSingle() * 2) - 1) * currentIntensity,
				((_shakeRandom.NextSingle() * 2) - 1) * currentIntensity
			);
		} else {
			_shakeOffset = Vector2.Zero;
		}

		// Clamp camera position to world bounds if set
		if (WorldBounds.HasValue) {
			Rectangle bounds = WorldBounds.Value;

			float minX = ViewportWidth / 2f / Zoom;
			float maxX = bounds.Width - (ViewportWidth / 2f / Zoom);
			float minY = ViewportHeight / 2f / Zoom;
			float maxY = bounds.Height - (ViewportHeight / 2f / Zoom);

			_basePosition = new Vector2(
				MathHelper.Clamp(_basePosition.X, minX, maxX),
				MathHelper.Clamp(_basePosition.Y, minY, maxY)
			);
		}

		// Build transformation matrix (uses Position getter which includes shake)
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
		Vector2 diff = targetPosition - _basePosition;  // Use _basePosition, not Position
		float distance = diff.Length();

		if (distance < DEADZONE_THRESHOLD) {
			_basePosition = targetPosition;
			return;
		}

		float smoothFactor = 1f - (float)Math.Pow(0.5f, SMOOTHING_SPEED * deltaTime);
		_basePosition = Vector2.Lerp(_basePosition, targetPosition, smoothFactor);
		_basePosition = new Vector2((float)Math.Round(_basePosition.X), (float)Math.Round(_basePosition.Y));
	}

	// Convert screen coordinates to world coordinates
	public Vector2 ScreenToWorld(Vector2 screenPosition) {
		return Vector2.Transform(screenPosition, Matrix.Invert(Transform));
	}

	public Vector2 ScreenToWorld(float x, float y) {
		return Vector2.Transform(new Vector2(x, y), Matrix.Invert(Transform));
	}

	// Convert world coordinates to screen coordinates
	public Vector2 WorldToScreen(Vector2 worldPosition) {
		return Vector2.Transform(worldPosition, Transform);
	}

	// Get the visible area of the world
	public Rectangle GetVisibleArea() {
		Matrix inverseTransform = Matrix.Invert(Transform);
		Vector2 topLeft = Vector2.Transform(Vector2.Zero, inverseTransform);
		Vector2 bottomRight = Vector2.Transform(
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