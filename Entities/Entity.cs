using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Candyland.Core;

namespace Candyland.Entities
{
    public abstract class Entity
    {
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
        public int Health { get; set; }
        public bool IsAlive => Health > 0;
        public int AttackDamage { get; set; }

        // Invincibility frames (prevents multiple hits in quick succession)
        protected float _invincibilityTimer = 0f;
        protected float _invincibilityDuration = 0.5f;
        public bool IsInvincible => _invincibilityTimer > 0;

        // Knockback
        protected Vector2 _knockbackVelocity = Vector2.Zero;
        protected float _knockbackDecay = 10f;

        public Entity(Texture2D texture, Vector2 position, int width, int height, float speed)
        {
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

        public Entity(Texture2D spriteSheet, Vector2 position, int frameCount, int frameWidth, int frameHeight, float frameTime, int width, int height, float speed, bool pingpong = false)
        {
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

        public abstract void Update(GameTime gameTime);

        protected void UpdateCombatTimers(float deltaTime)
        {
            // Update invincibility timer
            if (_invincibilityTimer > 0)
            {
                _invincibilityTimer -= deltaTime;
            }

            // Apply and decay knockback
            if (_knockbackVelocity.Length() > 0)
            {
                Position += _knockbackVelocity * deltaTime;
                _knockbackVelocity -= _knockbackVelocity * _knockbackDecay * deltaTime;

                if (_knockbackVelocity.Length() < 1f)
                {
                    _knockbackVelocity = Vector2.Zero;
                }
            }
        }

        // Apply knockback with collision checking
        public void ApplyKnockbackWithCollision(World.TileMap map)
        {
            if (_knockbackVelocity.Length() == 0 || map == null)
                return;

            // Check if knockback position would collide
            Rectangle potentialBounds = new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                Width,
                Height
            );

            if (map.checkCollision(potentialBounds))
            {
                // Cancel knockback if it would push into a wall
                _knockbackVelocity = Vector2.Zero;
            }
        }

        public virtual void TakeDamage(int damage, Vector2 attackerPosition)
        {
            if (IsInvincible || !IsAlive)
                return;

            Health -= damage;
            if (Health < 0)
                Health = 0;

            // Apply knockback away from attacker
            Vector2 knockbackDirection = Position - attackerPosition;
            if (knockbackDirection.Length() > 0)
            {
                knockbackDirection.Normalize();
                _knockbackVelocity = knockbackDirection * 300f; // Knockback strength
            }

            // Start invincibility frames
            _invincibilityTimer = _invincibilityDuration;

            if (!IsAlive)
            {
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            // Override in derived classes for death behavior
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!IsAlive) return;

            // Flash white when invincible
            Color tint = IsInvincible && (_invincibilityTimer * 10) % 1 > 0.5f ? Color.Red : Color.White;

            if (_useAnimation && _animationController != null)
            {
                var sourceRect = _animationController.GetSourceRectangle();
                Vector2 spritePosition = new Vector2(
                    Position.X + (Width - sourceRect.Width) / 2f,
                    Position.Y + (Height - sourceRect.Height) / 2f
                );
                spriteBatch.Draw(_animationController.GetTexture(), spritePosition, sourceRect, tint);
            }
            else
            {
                Vector2 spritePosition = new Vector2(
                    Position.X + (Width - _texture.Width) / 2f,
                    Position.Y + (Height - _texture.Height) / 2f
                );
                spriteBatch.Draw(_texture, spritePosition, tint);
            }
        }

        // Check if this entity collides with another
        public bool CollidesWith(Entity other)
        {
            return Bounds.Intersects(other.Bounds);
        }
    }
}