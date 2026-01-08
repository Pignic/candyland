using EldmeresTale.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Systems.VFX;

public class LevelUpEffect {
	public Vector2 Position { get; private set; }
	public bool IsExpired { get; private set; }

	private readonly float _lifetime = 2f;
	private float _timer = 0f;
	private readonly BitmapFont _font;
	private float _scale = 1f;

	public LevelUpEffect(Vector2 position, BitmapFont font) {
		Position = position;
		_font = font;
		IsExpired = false;
	}

	public void Update(GameTime gameTime) {
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		_timer += deltaTime;

		// Scale up then down
		_scale = 1f + ((float)System.Math.Sin(_timer * 5f) * 0.3f);

		// Float upward
		Position += new Vector2(0, -20f * deltaTime);

		// Check if expired
		if (_timer >= _lifetime) {
			IsExpired = true;
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (IsExpired) {
			return;
		}

		// Fade out over time
		float alpha = 1f - (_timer / _lifetime);
		Color drawColor = Color.Gold * alpha;

		const string text = "LEVEL UP";
		_font.DrawText(spriteBatch, text, Position, drawColor, _scale, true);
	}
}