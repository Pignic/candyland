using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Candyland.Core;

namespace Candyland.Entities
{
    public class DamageNumber
    {
        public Vector2 Position { get; private set; }
        public int Damage { get; private set; }
        public bool IsExpired { get; private set; }

        private float _lifetime = 1f;
        private float _timer = 0f;
        private Vector2 _velocity;
        private Color _color;
        private BitmapFont _font;
        private float _scale = 1f;

        public DamageNumber(int damage, Vector2 position, BitmapFont font, bool isPlayerDamage = false, Color? customColor = null)
        {
            Damage = damage;
            Position = position;
            _font = font;
            IsExpired = false;

            // Float upward
            _velocity = new Vector2(0, -50f);

            // Color based on who took damage or custom color
            if (customColor.HasValue)
            {
                _color = customColor.Value;
            }
            else
            {
                _color = isPlayerDamage ? Color.Red : Color.White;
            }

            // Scale up for big numbers
            if (damage >= 100)
            {
                _scale = 1.5f;
            }
            else if (damage >= 50)
            {
                _scale = 1.2f;
            }
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _timer += deltaTime;

            // Move upward
            Position += _velocity * deltaTime;

            // Slow down over time
            _velocity *= 0.95f;

            // Check if expired
            if (_timer >= _lifetime)
            {
                IsExpired = true;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsExpired) return;

            // Fade out over time
            float alpha = 1f - (_timer / _lifetime);
            Color drawColor = _color * alpha;

            string text = Damage.ToString();

            // Draw with scale
            if (_scale != 1f)
            {
                // For larger text, we'd need to draw each character scaled
                // For now, just draw normally
                _font.DrawText(spriteBatch, text, Position, drawColor);
            }
            else
            {
                _font.DrawText(spriteBatch, text, Position, drawColor);
            }
        }
    }
}