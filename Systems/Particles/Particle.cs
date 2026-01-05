using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Systems.Particles;

public class Particle {
	public Vector2 Position;
	public Vector2 Velocity;
	public Color Color;
	public float Size;
	public float Lifetime;
	public float Age;
	public float Rotation;
	public float RotationSpeed;
	public bool IsActive;

	// Physics
	public Vector2 Gravity = new Vector2(0, 200f);  // Pixels per second squared
	public float Drag = 0.98f;  // Velocity multiplier per frame

	public bool IsExpired => Age >= Lifetime || !IsActive;

	public void Reset() {
		Position = Vector2.Zero;
		Velocity = Vector2.Zero;
		Color = Color.White;
		Size = 1f;
		Lifetime = 1f;
		Age = 0f;
		Rotation = 0f;
		RotationSpeed = 0f;
		IsActive = true;
		Gravity = new Vector2(0, 200f);
		Drag = 0.98f;
	}

	public void Update(float deltaTime) {
		if (!IsActive) {
			return;
		}

		Age += deltaTime;

		// Apply physics
		Velocity += Gravity * deltaTime;
		Velocity *= Drag;
		Position += Velocity * deltaTime;
		Rotation += RotationSpeed * deltaTime;

		// Expire check
		if (Age >= Lifetime) {
			IsActive = false;
		}
	}

	public void Draw(SpriteBatch spriteBatch, Texture2D pixel) {
		if (!IsActive) {
			return;
		}

		// Calculate alpha fade
		float lifeRatio = Age / Lifetime;
		float alpha = 1f - lifeRatio;  // Fade out over lifetime

		// Draw particle
		Rectangle destRect = new Rectangle(
			(int)Position.X,
			(int)Position.Y,
			(int)Size,
			(int)Size
		);

		spriteBatch.Draw(
			pixel,
			destRect,
			null,
			Color * alpha,
			Rotation,
			new Vector2(0.5f),  // Origin at center
			SpriteEffects.None,
			0f
		);
	}
}