using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EldmeresTale.Core.UI;

namespace EldmeresTale.Entities
{
    public class LevelUpEffect
    {
        public Vector2 Position { get; private set; }
        public bool IsExpired { get; private set; }

        private float _lifetime = 2f;
        private float _timer = 0f;
        private BitmapFont _font;
        private float _scale = 1f;

        public LevelUpEffect(Vector2 position, BitmapFont font)
        {
            Position = position;
            _font = font;
            IsExpired = false;
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _timer += deltaTime;

            // Scale up then down
            _scale = 1f + (float)System.Math.Sin(_timer * 5f) * 0.3f;

            // Float upward
            Position += new Vector2(0, -20f * deltaTime);

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
            Color drawColor = Color.Gold * alpha;

            string text = "LEVEL UP";
            _font.drawText(spriteBatch, text, Position, drawColor);
        }
    }
}