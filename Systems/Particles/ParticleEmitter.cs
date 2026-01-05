using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Systems.Particles;

public enum ParticleType {
	Blood,           // Red splatter on hit
	Dust,            // Gray/brown dust on movement
	Sparkle,         // Yellow/white twinkles for pickups
	Destruction,     // Brown/gray chunks for breaking things
	Smoke,           // Gray wisps
	Fire,            // Orange/yellow flames
	Heal,            // Green sparkles
	Magic,           // Purple/blue energy
	Snow,            // White particles falling
	Rain             // Blue streaks falling
}

public static class ParticleEmitter {

	private static Random _random = new Random();

	public static void Emit(
		List<Particle> particles,
		ParticleType type,
		Vector2 position,
		int count = 10,
		Vector2? direction = null) {

		for (int i = 0; i < count; i++) {
			Particle p = GetInactiveParticle(particles);
			if (p == null) {
				p = new Particle();
				particles.Add(p);
			}

			// Reset and configure
			p.Reset();
			p.Position = position;

			// Configure based on type
			switch (type) {
				case ParticleType.Blood:
					ConfigureBlood(p, direction);
					break;
				case ParticleType.Dust:
					ConfigureDust(p, direction);
					break;
				case ParticleType.Sparkle:
					ConfigureSparkle(p);
					break;
				case ParticleType.Destruction:
					ConfigureDestruction(p);
					break;
				case ParticleType.Smoke:
					ConfigureSmoke(p, direction);
					break;
				case ParticleType.Fire:
					ConfigureFire(p);
					break;
				case ParticleType.Heal:
					ConfigureHeal(p);
					break;
				case ParticleType.Magic:
					ConfigureMagic(p, direction);
					break;
				case ParticleType.Snow:
					ConfigureSnow(p);
					break;
				case ParticleType.Rain:
					ConfigureRain(p);
					break;
			}
		}
	}

	private static Particle GetInactiveParticle(List<Particle> particles) {
		foreach (Particle p in particles) {
			if (!p.IsActive) {
				return p;
			}
		}
		return null;
	}

	private static void ConfigureBlood(Particle p, Vector2? direction) {
		// Red splatter
		p.Color = new Color(
			180 + _random.Next(76),   // 180-255 red
			0,
			0
		);
		p.Size = 2f + ((float)_random.NextDouble() * 3f);  // 2-5 pixels
		p.Lifetime = 0.3f + ((float)_random.NextDouble() * 0.3f);  // 0.3-0.6s

		// Splatter direction
		float angle = direction.HasValue
			? (float)Math.Atan2(direction.Value.Y, direction.Value.X)
			: (float)_random.NextDouble() * MathF.PI * 2f;

		angle += ((float)_random.NextDouble() - 0.5f) * 1.5f;  // Spread

		float speed = 50f + ((float)_random.NextDouble() * 100f);
		p.Velocity = new Vector2(
			MathF.Cos(angle) * speed,
			MathF.Sin(angle) * speed
		);

		p.Gravity = new Vector2(0, 300f);  // Falls quickly
		p.Drag = 0.95f;
	}

	private static void ConfigureDust(Particle p, Vector2? direction) {
		// Gray/brown dust
		byte gray = (byte)(100 + _random.Next(80));  // 100-180
		p.Color = new Color(gray, gray - 20, gray - 40);  // Brownish
		p.Size = 1f + ((float)_random.NextDouble() * 2f);  // 1-3 pixels
		p.Lifetime = 0.4f + ((float)_random.NextDouble() * 0.4f);  // 0.4-0.8s

		// Puff outward
		float angle = (float)_random.NextDouble() * MathF.PI * 2f;
		float speed = 20f + ((float)_random.NextDouble() * 40f);
		p.Velocity = new Vector2(
			MathF.Cos(angle) * speed,
			(MathF.Sin(angle) * speed) - 30f  // Slightly upward
		);

		p.Gravity = new Vector2(0, 50f);  // Light gravity
		p.Drag = 0.96f;
	}

	private static void ConfigureSparkle(Particle p) {
		// Yellow/white twinkles
		byte brightness = (byte)(200 + _random.Next(56));
		p.Color = new Color(brightness, brightness, brightness - 50);  // Yellowish white
		p.Size = 2f + ((float)_random.NextDouble() * 2f);  // 2-4 pixels
		p.Lifetime = 0.5f + ((float)_random.NextDouble() * 0.5f);  // 0.5-1.0s

		// Float upward with outward spread
		float angle = (float)_random.NextDouble() * MathF.PI * 2f;
		float speed = 15f + ((float)_random.NextDouble() * 25f);
		p.Velocity = new Vector2(
			MathF.Cos(angle) * speed,
			-50f - ((float)_random.NextDouble() * 30f)  // Upward
		);

		p.Gravity = new Vector2(0, -20f);  // Negative gravity (floats up!)
		p.Drag = 0.98f;
		p.RotationSpeed = ((float)_random.NextDouble() - 0.5f) * 5f;  // Spin
	}

	private static void ConfigureDestruction(Particle p) {
		// Brown/gray chunks
		byte shade = (byte)(80 + _random.Next(100));
		p.Color = new Color(shade + 20, shade, shade - 20);  // Brownish gray
		p.Size = 3f + ((float)_random.NextDouble() * 4f);  // 3-7 pixels (bigger chunks)
		p.Lifetime = 0.6f + ((float)_random.NextDouble() * 0.4f);  // 0.6-1.0s

		// Explode outward
		float angle = (float)_random.NextDouble() * MathF.PI * 2f;
		float speed = 60f + ((float)_random.NextDouble() * 80f);
		p.Velocity = new Vector2(
			MathF.Cos(angle) * speed,
			(MathF.Sin(angle) * speed) - 100f  // Initial upward burst
		);

		p.Gravity = new Vector2(0, 400f);  // Heavy gravity
		p.Drag = 0.97f;
		p.RotationSpeed = ((float)_random.NextDouble() - 0.5f) * 10f;  // Tumble
	}

	private static void ConfigureSmoke(Particle p, Vector2? direction) {
		// Gray wisps
		byte gray = (byte)(100 + _random.Next(60));
		p.Color = new Color(gray, gray, gray);
		p.Size = 3f + ((float)_random.NextDouble() * 5f);  // 3-8 pixels
		p.Lifetime = 0.8f + ((float)_random.NextDouble() * 0.6f);  // 0.8-1.4s

		// Waft upward
		float angle = direction.HasValue
			? (float)Math.Atan2(direction.Value.Y, direction.Value.X) - (MathF.PI / 2)
			: -MathF.PI / 2;  // Default upward

		angle += ((float)_random.NextDouble() - 0.5f) * 0.8f;  // Spread

		float speed = 20f + ((float)_random.NextDouble() * 30f);
		p.Velocity = new Vector2(
			MathF.Cos(angle) * speed,
			MathF.Sin(angle) * speed
		);

		p.Gravity = new Vector2(0, -30f);  // Floats up
		p.Drag = 0.99f;  // Very slow drag
	}

	private static void ConfigureFire(Particle p) {
		// Orange/yellow flames
		bool isYellow = _random.NextDouble() > 0.5;
		p.Color = isYellow
			? new Color(255, 255, 150)  // Yellow
			: new Color(255, 140, 0);    // Orange

		p.Size = 2f + ((float)_random.NextDouble() * 3f);  // 2-5 pixels
		p.Lifetime = 0.3f + ((float)_random.NextDouble() * 0.3f);  // 0.3-0.6s (quick)

		// Rise upward with flicker
		p.Velocity = new Vector2(
			((float)_random.NextDouble() - 0.5f) * 20f,  // Horizontal wobble
			-80f - ((float)_random.NextDouble() * 40f)     // Upward
		);

		p.Gravity = new Vector2(0, -50f);  // Strong upward pull
		p.Drag = 0.96f;
	}

	private static void ConfigureHeal(Particle p) {
		// Green sparkles
		p.Color = new Color(100, 255, 150);  // Bright green
		p.Size = 2f + ((float)_random.NextDouble() * 2f);  // 2-4 pixels
		p.Lifetime = 0.6f + ((float)_random.NextDouble() * 0.4f);  // 0.6-1.0s

		// Float upward gently
		float angle = (float)_random.NextDouble() * MathF.PI * 2f;
		float speed = 10f + ((float)_random.NextDouble() * 20f);
		p.Velocity = new Vector2(
			MathF.Cos(angle) * speed,
			-30f - ((float)_random.NextDouble() * 20f)
		);

		p.Gravity = new Vector2(0, -15f);  // Gentle float
		p.Drag = 0.98f;
		p.RotationSpeed = ((float)_random.NextDouble() - 0.5f) * 4f;
	}

	private static void ConfigureMagic(Particle p, Vector2? direction) {
		// Purple/blue energy
		bool isPurple = _random.NextDouble() > 0.5;
		p.Color = isPurple
			? new Color(200, 100, 255)  // Purple
			: new Color(100, 150, 255); // Blue

		p.Size = 2f + ((float)_random.NextDouble() * 3f);  // 2-5 pixels
		p.Lifetime = 0.5f + ((float)_random.NextDouble() * 0.5f);  // 0.5-1.0s

		// Swirl effect
		float angle = direction.HasValue
			? (float)Math.Atan2(direction.Value.Y, direction.Value.X)
			: (float)_random.NextDouble() * MathF.PI * 2f;

		angle += ((float)_random.NextDouble() - 0.5f) * 2f;

		float speed = 40f + ((float)_random.NextDouble() * 60f);
		p.Velocity = new Vector2(
			MathF.Cos(angle) * speed,
			MathF.Sin(angle) * speed
		);

		p.Gravity = Vector2.Zero;  // No gravity (magic!)
		p.Drag = 0.94f;  // Slows quickly
		p.RotationSpeed = ((float)_random.NextDouble() - 0.5f) * 8f;
	}

	private static void ConfigureSnow(Particle p) {
		// White flakes
		p.Color = Color.White;
		p.Size = 1f + ((float)_random.NextDouble() * 2f);  // 1-3 pixels
		p.Lifetime = 3f + ((float)_random.NextDouble() * 2f);  // 3-5s (long)

		// Gentle fall with sway
		p.Velocity = new Vector2(
			((float)_random.NextDouble() - 0.5f) * 20f,  // Horizontal drift
			20f + ((float)_random.NextDouble() * 20f)      // Downward
		);

		p.Gravity = new Vector2(0, 30f);  // Light gravity
		p.Drag = 0.995f;  // Very slow drag
	}

	private static void ConfigureRain(Particle p) {
		// Blue streaks
		p.Color = new Color(150, 180, 255);  // Light blue
		p.Size = 1f + (float)_random.NextDouble();  // 1-2 pixels (thin)
		p.Lifetime = 1f + ((float)_random.NextDouble() * 0.5f);  // 1-1.5s

		// Fast downward
		p.Velocity = new Vector2(
			((float)_random.NextDouble() - 0.5f) * 40f,  // Slight horizontal
			200f + ((float)_random.NextDouble() * 100f)    // Fast down
		);

		p.Gravity = new Vector2(0, 500f);  // Strong gravity
		p.Drag = 1f;  // No drag (constant speed)
	}
}