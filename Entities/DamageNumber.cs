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

        public DamageNumber(int damage, Vector2 position, BitmapFont font, bool isPlayerDamage = false)
        {
            Damage = damage;
            Position = position;
            _font = font;
            IsExpired = false;

            // Float upward
            _velocity = new Vector2(0, -50f);

            // Color based on who took damage
            _color = isPlayerDamage ? Color.Red : Color.White;
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
            _font.DrawText(spriteBatch, text, Position, drawColor);
        }
    }
}