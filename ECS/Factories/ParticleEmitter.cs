using DefaultEcs;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Factories;

public class ParticleEmitter {
	private readonly World _world;
	private readonly Random _random;

	public ParticleEmitter(World world) {
		_world = world;
		_random = new Random();
	}

	public void SpawnBurst(Vector2 position, Color color, int count = 10, float speed = 100f, float size = 3f) {
		for (int i = 0; i < count; i++) {
			// Random direction
			float angle = (float)(_random.NextDouble() * Math.PI * 2);
			float velocitySpeed = (float)((_random.NextDouble() * speed) + (speed * 0.5f));
			Vector2 velocity = new Vector2(
				MathF.Cos(angle) * velocitySpeed,
				MathF.Sin(angle) * velocitySpeed
			);

			CreateParticle(position, velocity, color, size, lifetime: 0.5f);
		}
	}

	public void SpawnBloodSplatter(Vector2 position, Vector2 direction, int count = 15) {
		for (int i = 0; i < count; i++) {
			// Spread around direction
			float spreadAngle = (float)(_random.NextDouble() - 0.5) * 0.8f;
			float angle = MathF.Atan2(direction.Y, direction.X) + spreadAngle;

			float speed = (float)((_random.NextDouble() * 100) + 50);
			Vector2 velocity = new Vector2(
				MathF.Cos(angle) * speed,
				(MathF.Sin(angle) * speed) - 50  // Initial upward velocity
			);

			// Random red shades
			Color bloodColor = new Color(
				180 + _random.Next(75),
				0,
				0,
				255
			);

			float size = (float)((_random.NextDouble() * 2) + 2);
			float lifetime = 0.3f + ((float)_random.NextDouble() * 0.3f);
			Entity entity = CreateParticle(position, velocity, bloodColor, size, lifetime: lifetime);
			entity.Set(new Gravity(300f));  // Blood falls
		}
	}

	public void SpawnDustCloud(Vector2 position, int count = 8) {
		for (int i = 0; i < count; i++) {
			float angle = (float)(_random.NextDouble() * Math.PI * 2);
			float speed = (float)((_random.NextDouble() * 30) + 20);
			Vector2 velocity = new Vector2(
				MathF.Cos(angle) * speed,
				MathF.Sin(angle) * speed
			);

			Color dustColor = new Color(200, 200, 200, 150);
			float size = (float)((_random.NextDouble() * 4) + 3);

			CreateParticle(position, velocity, dustColor, size, lifetime: 0.6f, fadeSize: true);
		}
	}

	public void SpawnImpactSparks(Vector2 position, Vector2 impactNormal, int count = 12) {
		for (int i = 0; i < count; i++) {
			// Reflect around impact normal
			float baseAngle = MathF.Atan2(impactNormal.Y, impactNormal.X);
			float spreadAngle = (float)(_random.NextDouble() - 0.5) * 1.5f;
			float angle = baseAngle + spreadAngle;

			float speed = (float)((_random.NextDouble() * 150) + 100);
			Vector2 velocity = new Vector2(
				MathF.Cos(angle) * speed,
				MathF.Sin(angle) * speed
			);

			// Yellow to orange
			Color sparkColor = _random.NextDouble() > 0.5
				? Color.Yellow
				: Color.Orange;

			float size = (float)((_random.NextDouble() * 1.5) + 1);

			Entity entity = CreateParticle(position, velocity, sparkColor, size, lifetime: 0.3f);
			entity.Set(new Gravity(200f));  // Light gravity
		}
	}

	public void SpawnHealingParticles(Vector2 position, int count = 10) {
		for (int i = 0; i < count; i++) {
			float angle = (float)(_random.NextDouble() * Math.PI * 2);
			float speed = (float)((_random.NextDouble() * 20) + 10);
			Vector2 velocity = new Vector2(
				MathF.Cos(angle) * speed,
				-50 - ((float)_random.NextDouble() * 30)  // Float upward
			);

			Color healColor = Color.LimeGreen;
			float size = (float)((_random.NextDouble() * 3) + 2);

			CreateParticle(position, velocity, healColor, size, lifetime: 1f, fadeSize: true);
		}
	}

	private Entity CreateParticle(Vector2 position, Vector2 velocity, Color color, float size, float lifetime, bool fadeSize = false) {
		Entity entity = _world.CreateEntity();

		entity.Set(new Position(position));
		entity.Set(new Velocity(velocity));
		entity.Set(new ParticleData(color, size, fadeSpeed: 1f / lifetime, fadeSize: fadeSize));
		entity.Set(new Lifetime(lifetime));

		return entity;
	}
}