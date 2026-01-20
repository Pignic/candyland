using EldmeresTale.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Entities;

public abstract class BaseEntity {
	// Properties
	public Vector2 Position { get; set; }
	public float Speed { get; set; }

	// Rendering
	protected Texture2D _texture;
	protected AnimationController _animationController;
	protected bool _useAnimation;

	// Size
	public int Width { get; set; }
	public int Height { get; set; }

	// Collision
	public Rectangle Bounds => new Rectangle(
		(int)Position.X,
		(int)Position.Y,
		Width,
		Height
	);

	// Health & Combat
	public int MaxHealth { get; set; }
	public int Health { get; set; }
	public int AttackDamage { get; set; }

	// Knockback
	protected Vector2 _knockbackVelocity = Vector2.Zero;
	protected float _knockbackDecay = 10f;

	protected BaseEntity(Texture2D texture, Vector2 position, int width, int height, float speed) {
		_texture = texture;
		Position = position;
		Width = width;
		Height = height;
		Speed = speed;
		_useAnimation = false;

		// Default health values
		MaxHealth = 100;
		Health = MaxHealth;
		AttackDamage = 10;
	}

	protected BaseEntity(Texture2D spriteSheet, Vector2 position, int frameCount, int frameWidth, int frameHeight, float frameTime, int width, int height, float speed, bool pingpong = false) {
		_texture = spriteSheet;
		_animationController = new AnimationController(spriteSheet, frameCount, frameWidth, frameHeight, frameTime, pingpong);
		Position = position;
		Width = width;
		Height = height;
		Speed = speed;
		_useAnimation = true;

		// Default health values
		MaxHealth = 100;
		Health = MaxHealth;
		AttackDamage = 10;
	}

	public virtual void Update(GameTime gameTime) {

	}

	protected virtual void OnDeath() {
		// Override in derived classes for death behavior
	}


	public virtual void Draw(SpriteBatch spriteBatch) {
	}

	protected virtual void DrawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? sourceRect, Color tint) {
		spriteBatch.Draw(texture, position, sourceRect, tint);
	}

	// Check if this entity collides with another
	public bool CollidesWith(BaseEntity other) {
		return Bounds.Intersects(other.Bounds);
	}
}