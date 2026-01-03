using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EldmeresTale.Core;

namespace EldmeresTale.Entities; 
public abstract class Entity {
	// Properties
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; protected set; }
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
	public int health { get; set; }
	public bool IsAlive => health > 0;
	public int AttackDamage { get; set; }

	// Invincibility frames (prevents multiple hits in quick succession)
	protected float _invincibilityTimer = 0f;
	protected float _invincibilityDuration = 0.5f;
	public bool IsInvincible => _invincibilityTimer > 0;

	// Knockback
	protected Vector2 _knockbackVelocity = Vector2.Zero;
	protected float _knockbackDecay = 10f;

	public Entity(Texture2D texture, Vector2 position, int width, int height, float speed) {
		_texture = texture;
		Position = position;
		Width = width;
		Height = height;
		Speed = speed;
		_useAnimation = false;

		// Default health values
		MaxHealth = 100;
		health = MaxHealth;
		AttackDamage = 10;
	}

	public Entity(Texture2D spriteSheet, Vector2 position, int frameCount, int frameWidth, int frameHeight, float frameTime, int width, int height, float speed, bool pingpong = false) {
		_texture = spriteSheet;
		_animationController = new AnimationController(spriteSheet, frameCount, frameWidth, frameHeight, frameTime, pingpong);
		Position = position;
		Width = width;
		Height = height;
		Speed = speed;
		_useAnimation = true;

		// Default health values
		MaxHealth = 100;
		health = MaxHealth;
		AttackDamage = 10;
	}

	public virtual void Update(GameTime gameTime) {

	}



	public virtual void TakeDamage(int damage, Vector2 attackerPosition) {
		if(IsInvincible || !IsAlive)
			return;

		health -= damage;
		if(health < 0)
			health = 0;

		// Apply knockback away from attacker
		Vector2 knockbackDirection = Position - attackerPosition;
		if(knockbackDirection.Length() > 0) {
			knockbackDirection.Normalize();
			_knockbackVelocity = knockbackDirection * 300f; // Knockback strength
		}

		// Start invincibility frames
		_invincibilityTimer = _invincibilityDuration;

		if(!IsAlive) {
			OnDeath();
		}
	}

	protected virtual void OnDeath() {
		// Override in derived classes for death behavior
	}

	protected virtual Color getTint() {
		return IsInvincible && (_invincibilityTimer * 10) % 1 > 0.5f ? Color.Red : Color.White;
	}

	public virtual void Draw(SpriteBatch spriteBatch) {
		if(!IsAlive) return;

		// Flash white when invincible
		Color tint = getTint();

		if(_useAnimation && _animationController != null) {
			var sourceRect = _animationController.GetSourceRectangle();
			Vector2 spritePosition = new Vector2(
				Position.X + (Width - sourceRect.Width) / 2f,
				Position.Y + (Height - sourceRect.Height) / 2f
			);
			spriteBatch.Draw(_animationController.GetTexture(), spritePosition, sourceRect, tint);
		} else {
			Vector2 spritePosition = new Vector2(
				Position.X + (Width - _texture.Width) / 2f,
				Position.Y + (Height - _texture.Height) / 2f
			);
			spriteBatch.Draw(_texture, spritePosition, tint);
		}
	}

	// Check if this entity collides with another
	public bool CollidesWith(Entity other) {
		return Bounds.Intersects(other.Bounds);
	}
}