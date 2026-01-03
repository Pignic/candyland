using EldmeresTale.Systems.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

/// <summary>
/// Manages all particle effects in the game
/// </summary>
public class ParticleSystem : GameSystem {
	private readonly List<Particle> _particles;
	private readonly GraphicsDevice _graphicsDevice;
	private Texture2D _pixelTexture;

	// Performance settings
	private const int MAX_PARTICLES = 1000;

	public ParticleSystem(GraphicsDevice graphicsDevice) {
		_graphicsDevice = graphicsDevice;
		_particles = new List<Particle>();
		Enabled = true;
		Visible = true;
	}

	public override void Initialize() {
		// Create pixel texture for drawing particles
		_pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
		_pixelTexture.SetData(new[] { Color.White });

		System.Diagnostics.Debug.WriteLine("[PARTICLE SYSTEM] Initialized");
	}

	/// <summary>
	/// Emit particles at a position
	/// </summary>
	public void Emit(ParticleType type, Vector2 position, int count = 10, Vector2? direction = null) {
		// Don't exceed max particles
		int activeCount = 0;
		foreach(var p in _particles) {
			if(p.IsActive) activeCount++;
		}

		if(activeCount >= MAX_PARTICLES) {
			System.Diagnostics.Debug.WriteLine($"[PARTICLES] Max particles ({MAX_PARTICLES}) reached!");
			return;
		}

		ParticleEmitter.Emit(_particles, type, position, count, direction);
	}

	public override void Update(GameTime gameTime) {
		if(!Enabled) return;

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		// Update all particles
		foreach(var particle in _particles) {
			if(particle.IsActive) {
				particle.Update(deltaTime);
			}
		}

		// Clean up expired particles periodically
		if(_particles.Count > MAX_PARTICLES * 1.5f) {
			_particles.RemoveAll(p => p.IsExpired);
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if(!Visible || _pixelTexture == null) return;

		// Draw all active particles
		foreach(var particle in _particles) {
			if(particle.IsActive) {
				particle.Draw(spriteBatch, _pixelTexture);
			}
		}
	}

	public override void Dispose() {
		_pixelTexture?.Dispose();
		_particles.Clear();
		System.Diagnostics.Debug.WriteLine("[PARTICLE SYSTEM] Disposed");
	}

	/// <summary>
	/// Clear all particles
	/// </summary>
	public void Clear() {
		foreach(var p in _particles) {
			p.IsActive = false;
		}
	}

	/// <summary>
	/// Get count of active particles
	/// </summary>
	public int ActiveParticleCount {
		get {
			int count = 0;
			foreach(var p in _particles) {
				if(p.IsActive) count++;
			}
			return count;
		}
	}
}