using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Candyland.World;
using System;

namespace Candyland.Entities
{
    public enum EnemyBehavior
    {
        Idle,           // Stands still
        Patrol,         // Walks back and forth
        Chase,          // Follows the player when in range
        Wander          // Random wandering
    }

    public class Enemy : Entity
    {
        public EnemyBehavior Behavior { get; set; }
        public float DetectionRange { get; set; } = 150f;

        private Vector2 _patrolStart;
        private Vector2 _patrolEnd;
        private Vector2 _targetPosition;
        private bool _movingToEnd = true;

        private float _wanderTimer;
        private float _wanderInterval = 2f;
        private Random _random;

        // Reference to player for chase behavior
        private Entity _chaseTarget;
        private DualGridTileMap _map;

        // Drop chances (0.0 to 1.0)
        public float HealthDropChance { get; set; } = 0.3f;
        public float CoinDropChance { get; set; } = 0.8f;
        public bool HasDroppedLoot { get; set; } = false;
        public int XPValue { get; set; } = 50;

        // Static sprite constructor
        public Enemy(Texture2D texture, Vector2 position, EnemyBehavior behavior, int width = 24, int height = 24, float speed = 100f)
            : base(texture, position, width, height, speed)
        {
            Behavior = behavior;
            Initialize();
        }

        // Animated sprite constructor
        public Enemy(Texture2D spriteSheet, Vector2 position, EnemyBehavior behavior, int frameCount, int frameWidth, int frameHeight, float frameTime, int width = 24, int height = 24, float speed = 100f)
            : base(spriteSheet, position, frameCount, frameWidth, frameHeight, frameTime, width, height, speed)
        {
            Behavior = behavior;
            Initialize();
        }

        private void Initialize()
        {
            _random = new Random(Position.GetHashCode());
            _patrolStart = Position;
            _patrolEnd = Position + new Vector2(100, 0); // Default patrol
            _wanderTimer = _wanderInterval; // Start with full timer so it picks direction immediately
        }

        public void SetPatrolPoints(Vector2 start, Vector2 end)
        {
            _patrolStart = start;
            _patrolEnd = end;
        }

        public void SetChaseTarget(Entity target, DualGridTileMap map)
        {
            _chaseTarget = target;
            _map = map;
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsAlive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update combat timers (invincibility, knockback)
            UpdateCombatTimers(deltaTime);

            // Check knockback collision
            if (_map != null)
            {
                ApplyKnockbackWithCollision(_map);
            }


            switch (Behavior)
            {
                case EnemyBehavior.Idle:
                    Velocity = Vector2.Zero;
                    break;

                case EnemyBehavior.Patrol:
                    UpdatePatrol(deltaTime);
                    break;

                case EnemyBehavior.Wander:
                    UpdateWander(deltaTime);
                    break;

                case EnemyBehavior.Chase:
                    if (_chaseTarget != null)
                    {
                        UpdateChase(deltaTime);
                    }
                    break;
            }

            // Update animation
            if (_useAnimation && _animationController != null)
            {
                _animationController.Update(gameTime, Velocity);
            }
        }

        private void UpdatePatrol(float deltaTime)
        {
            Vector2 target = _movingToEnd ? _patrolEnd : _patrolStart;
            Vector2 direction = target - Position;

            if (direction.Length() < 5f)
            {
                _movingToEnd = !_movingToEnd;
            }
            else
            {
                direction.Normalize();
                Velocity = direction * Speed;
                Position += Velocity * deltaTime;
            }
        }

        private void UpdateWander(float deltaTime)
        {
            _wanderTimer -= deltaTime;

            if (_wanderTimer <= 0)
            {
                // Pick a new random direction
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Velocity = direction * Speed;

                _wanderTimer = _wanderInterval;
            }

            Position += Velocity * deltaTime;
        }

        private void UpdateChase(float deltaTime)
        {
            Vector2 targetPosition = _chaseTarget.Position + new Vector2(_chaseTarget.Width / 2f, _chaseTarget.Height / 2f);
            Vector2 direction = targetPosition - Position;
            float distance = direction.Length();

            if (distance > 10f && distance < DetectionRange)
            {
                direction.Normalize();
                Velocity = direction * Speed;

                Vector2 newPosition = Position + Velocity * deltaTime;

                // Check collision if map provided
                if (_map != null)
                {
                    Rectangle horizontalBounds = new Rectangle((int)newPosition.X, (int)Position.Y, Width, Height);
                    if (!_map.checkCollision(horizontalBounds))
                    {
                        Position = new Vector2(newPosition.X, Position.Y);
                    }

                    Rectangle verticalBounds = new Rectangle((int)Position.X, (int)newPosition.Y, Width, Height);
                    if (!_map.checkCollision(verticalBounds))
                    {
                        Position = new Vector2(Position.X, newPosition.Y);
                    }
                }
                else
                {
                    Position = newPosition;
                }
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }

        public void ChaseTarget(Vector2 targetPosition, float deltaTime, TileMap map = null)
        {
            Vector2 direction = targetPosition - Position;
            float distance = direction.Length();

            if (distance > 10f && distance < DetectionRange)
            {
                direction.Normalize();
                Velocity = direction * Speed;

                Vector2 newPosition = Position + Velocity * deltaTime;

                // Check collision if map provided
                if (map != null)
                {
                    Rectangle horizontalBounds = new Rectangle((int)newPosition.X, (int)Position.Y, Width, Height);
                    if (!map.checkCollision(horizontalBounds))
                    {
                        Position = new Vector2(newPosition.X, Position.Y);
                    }

                    Rectangle verticalBounds = new Rectangle((int)Position.X, (int)newPosition.Y, Width, Height);
                    if (!map.checkCollision(verticalBounds))
                    {
                        Position = new Vector2(Position.X, newPosition.Y);
                    }
                }
                else
                {
                    Position = newPosition;
                }
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }

        public void ApplyCollisionConstraints(DualGridTileMap map)
        {
            // Check if enemy is in a collision and needs to bounce
            if (map != null && map.checkCollision(Bounds))
            {
                // For patrol/wander, reverse direction on collision
                if (Behavior == EnemyBehavior.Patrol)
                {
                    _movingToEnd = !_movingToEnd;
                }
                else if (Behavior == EnemyBehavior.Wander)
                {
                    // Pick a new random direction immediately
                    float angle = (float)(_random.NextDouble() * Math.PI * 2);
                    Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Velocity = direction * Speed;
                    _wanderTimer = _wanderInterval;
                }
            }
        }
    }
}