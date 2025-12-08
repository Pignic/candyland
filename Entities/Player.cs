using Candyland.Core;
using Candyland.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Candyland.Entities
{
    public class Player : Entity
    {
        // Attack properties
        private float _attackCooldown = 0f;
        private float _attackCooldownDuration = 0.5f;
        private float _attackRange = 40f;
        private bool _isAttacking = false;
        private float _attackDuration = 0.2f;
        private float _attackTimer = 0f;
        private Vector2 _lastMoveDirection = new Vector2(0, 1); // Default: down

        // Track which enemies have been hit this attack
        private System.Collections.Generic.HashSet<Entity> _hitThisAttack = new System.Collections.Generic.HashSet<Entity>();

        // Attack effect
        private AttackEffect _attackEffect;

        public bool CanAttack => _attackCooldown <= 0 && !_isAttacking;

        // Player stats
        public int Coins { get; set; } = 0;
        public int Level { get; private set; } = 1;
        public int XP { get; private set; } = 0;
        public int XPToNextLevel { get; private set; } = 100;

        // Stat bonuses per level
        private const int HEALTH_PER_LEVEL = 20;
        private const int DAMAGE_PER_LEVEL = 5;
        private const float SPEED_PER_LEVEL = 10f;

        // Attack hitbox (in front of player)
        public Rectangle AttackBounds
        {
            get
            {
                if (!_isAttacking)
                    return Rectangle.Empty;

                Vector2 center = Position + new Vector2(Width / 2f, Height / 2f);
                int hitboxSize = 30;

                // Use last move direction to determine attack direction
                Vector2 attackOffset = _lastMoveDirection * _attackRange;
                Vector2 attackPos = center + attackOffset;

                return new Rectangle(
                    (int)(attackPos.X - hitboxSize / 2),
                    (int)(attackPos.Y - hitboxSize / 2),
                    hitboxSize,
                    hitboxSize
                );
            }
        }

        // Constructor for static sprite
        public Player(Texture2D texture, Vector2 startPosition, int width = 24, int height = 24)
            : base(texture, startPosition, width, height, 200f)
        {
            MaxHealth = 100;
            Health = MaxHealth;
            AttackDamage = 25;
            Level = 1;
            XP = 0;
            XPToNextLevel = 100;
        }

        // Constructor for animated sprite
        public Player(Texture2D spriteSheet, Vector2 startPosition, int frameCount, int frameWidth, int frameHeight, float frameTime, int width = 24, int height = 24)
            : base(spriteSheet, startPosition, frameCount, frameWidth, frameHeight, frameTime, width, height, 200f)
        {
            MaxHealth = 100;
            Health = MaxHealth;
            AttackDamage = 25;
            Level = 1;
            XP = 0;
            XPToNextLevel = 100;
        }

        public void InitializeAttackEffect(Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice)
        {
            _attackEffect = new AttackEffect(graphicsDevice);
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update combat timers
            UpdateCombatTimers(deltaTime);

            // Update attack cooldown
            if (_attackCooldown > 0)
                _attackCooldown -= deltaTime;

            // Update attack animation
            if (_isAttacking)
            {
                _attackTimer -= deltaTime;
                if (_attackTimer <= 0)
                {
                    _isAttacking = false;
                    _hitThisAttack.Clear(); // Clear hit list when attack ends
                }
            }

            HandleInput(gameTime);

            // Update animation if using one
            if (_useAnimation && _animationController != null)
            {
                _animationController.Update(gameTime, Velocity);
            }
        }

        public void Update(GameTime gameTime, TileMap map)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update combat timers
            UpdateCombatTimers(deltaTime);

            // Check knockback collision
            ApplyKnockbackWithCollision(map);

            // Update attack cooldown
            if (_attackCooldown > 0)
                _attackCooldown -= deltaTime;

            // Update attack animation
            if (_isAttacking)
            {
                _attackTimer -= deltaTime;
                if (_attackTimer <= 0)
                {
                    _isAttacking = false;
                    _hitThisAttack.Clear(); // Clear hit list when attack ends
                }
            }

            // Update attack effect
            if (_attackEffect != null)
            {
                _attackEffect.Update(gameTime);
            }

            HandleInput(gameTime, map);

            // Update animation if using one
            if (_useAnimation && _animationController != null)
            {
                _animationController.Update(gameTime, Velocity);
            }
        }

        public void Attack()
        {
            if (CanAttack)
            {
                _isAttacking = true;
                _attackTimer = _attackDuration;
                _attackCooldown = _attackCooldownDuration;
                _hitThisAttack.Clear(); // Clear the list for the new attack

                // Trigger visual slash effect
                if (_attackEffect != null)
                {
                    Vector2 attackPos = Position + new Vector2(Width / 2f, Height / 2f);
                    attackPos += _lastMoveDirection * _attackRange;
                    _attackEffect.Trigger(attackPos, _lastMoveDirection);
                }
            }
        }

        public void DrawAttackEffect(SpriteBatch spriteBatch)
        {
            if (_attackEffect != null)
            {
                _attackEffect.Draw(spriteBatch);
            }
        }

        public bool HasHitEntity(Entity entity)
        {
            return _hitThisAttack.Contains(entity);
        }

        public void MarkEntityAsHit(Entity entity)
        {
            _hitThisAttack.Add(entity);
        }

        private void HandleInput(GameTime gameTime, TileMap map = null)
        {
            var keyboardState = Keyboard.GetState();
            var movement = Vector2.Zero;

            // Attack input (Space bar)
            if (keyboardState.IsKeyDown(Keys.Space))
            {
                Attack();
            }

            // Gather movement input
            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
                movement.Y -= 1;
            if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
                movement.Y += 1;
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
                movement.X -= 1;
            if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
                movement.X += 1;

            // Normalize diagonal movement
            if (movement.Length() > 0)
            {
                movement.Normalize();
                _lastMoveDirection = movement; // Track direction for attacks
            }

            // Calculate velocity
            Velocity = movement * Speed;

            // Apply movement with collision detection
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector2 newPosition = Position + Velocity * deltaTime;

            // If we have a map, check collision
            if (map != null)
            {
                // Try horizontal movement
                Rectangle horizontalBounds = new Rectangle(
                    (int)newPosition.X,
                    (int)Position.Y,
                    Width,
                    Height
                );

                if (!map.CheckCollision(horizontalBounds))
                {
                    Position = new Vector2(newPosition.X, Position.Y);
                }

                // Try vertical movement
                Rectangle verticalBounds = new Rectangle(
                    (int)Position.X,
                    (int)newPosition.Y,
                    Width,
                    Height
                );

                if (!map.CheckCollision(verticalBounds))
                {
                    Position = new Vector2(Position.X, newPosition.Y);
                }
            }
            else
            {
                // No collision detection, just move
                Position = newPosition;
            }
        }

        public void CollectPickup(Pickup pickup)
        {
            if (pickup.HealthRestore > 0)
            {
                Health = Math.Min(Health + pickup.HealthRestore, MaxHealth);
            }

            if (pickup.CoinValue > 0)
            {
                Coins += pickup.CoinValue;
            }

            pickup.Collect();
        }

        public bool GainXP(int amount)
        {
            XP += amount;

            // Check for level up
            if (XP >= XPToNextLevel)
            {
                LevelUp();
                return true; // Return true if leveled up
            }

            return false;
        }

        private void LevelUp()
        {
            Level++;
            XP -= XPToNextLevel;

            // Increase XP requirement for next level
            XPToNextLevel = (int)(XPToNextLevel * 1.5f);

            // Increase stats
            MaxHealth += HEALTH_PER_LEVEL;
            Health = MaxHealth; // Fully heal on level up
            AttackDamage += DAMAGE_PER_LEVEL;
            Speed += SPEED_PER_LEVEL;
        }

        // Useful for collision detection later
        public void ClampToScreen(int screenWidth, int screenHeight)
        {
            Position = new Vector2(
                MathHelper.Clamp(Position.X, 0, screenWidth - Width),
                MathHelper.Clamp(Position.Y, 0, screenHeight - Height)
            );
        }
    }
}