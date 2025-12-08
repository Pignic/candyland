using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Candyland.Entities
{
    public class AttackEffect
    {
        public Vector2 Position { get; set; }
        public bool IsActive { get; private set; }

        private Texture2D _slashTexture;
        private float _rotation;
        private float _duration = 0.2f;
        private float _timer = 0f;
        private float _scale = 1f;
        private Vector2 _direction;

        public AttackEffect(GraphicsDevice graphicsDevice)
        {
            _slashTexture = CreateSlashTexture(graphicsDevice);
            IsActive = false;
        }

        public void Trigger(Vector2 position, Vector2 direction)
        {
            Position = position;
            _direction = direction;
            IsActive = true;
            _timer = 0f;
            _scale = 0.5f;

            // Calculate rotation based on direction (handles diagonals too)
            _rotation = (float)System.Math.Atan2(direction.Y, direction.X) + MathHelper.Pi;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _timer += deltaTime;

            // Scale up quickly
            _scale = MathHelper.Lerp(0.5f, 1.5f, _timer / _duration);

            if (_timer >= _duration)
            {
                IsActive = false;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            // Fade out over time
            float alpha = 1f - (_timer / _duration);
            Color color = Color.White * alpha;

            // Draw slash with rotation
            Vector2 origin = new Vector2(_slashTexture.Width / 2, _slashTexture.Height / 2);

            spriteBatch.Draw(
                _slashTexture,
                Position,
                null,
                color,
                _rotation,
                origin,
                _scale,
                SpriteEffects.None,
                0f
            );
        }

        private Texture2D CreateSlashTexture(GraphicsDevice graphicsDevice)
        {
            int width = 48;
            int height = 48;
            Texture2D texture = new Texture2D(graphicsDevice, width, height);
            Color[] data = new Color[width * height];

            // Create an arc slash effect
            Vector2 center = new Vector2(width / 2, height / 2);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Vector2 pos = new Vector2(x, y);
                    Vector2 diff = pos - center;

                    float distance = diff.Length();
                    float angle = (float)System.Math.Atan2(diff.Y, diff.X);

                    // Normalize angle to 0-2PI
                    if (angle < 0) angle += MathHelper.TwoPi;

                    // Create arc slash (45 degree arc)
                    bool inArc = angle >= MathHelper.PiOver4 * 3 && angle <= MathHelper.PiOver4 * 5;

                    // Create thickness based on distance
                    bool inRange = distance > 12 && distance < 28;

                    if (inArc && inRange)
                    {
                        // Gradient from white (inner) to cyan (outer)
                        float distPercent = (distance - 12) / 16f;
                        Color slashColor = Color.Lerp(Color.White, Color.Cyan, distPercent);

                        // Add some transparency variation
                        float alpha = 1f - (distPercent * 0.5f);
                        data[index] = slashColor * alpha;
                    }
                    else
                    {
                        data[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }
    }
}