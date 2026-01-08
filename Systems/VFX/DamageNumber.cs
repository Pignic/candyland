using EldmeresTale.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Systems.VFX;

public class DamageNumber {
	public Vector2 Position { get; private set; }
	public int Damage { get; }
	public bool IsExpired { get; private set; }

	private readonly float _lifetime = 1f;
	private float _timer = 0f;
	private Vector2 _velocity;
	private Color _color;
	private readonly BitmapFont _font;
	private readonly float _scale = 1f;
	private readonly bool _isCrit;
	private readonly Random _random = new Random();

	public DamageNumber(int damage, Vector2 position, BitmapFont font, bool isCrit = false, Color? customColor = null) {
		Damage = damage;
		_isCrit = isCrit;
		_font = font;
		IsExpired = false;

		// Add random offset to position (prevents stacking)
		float randomX = (float)((_random.NextDouble() * 10) - 5);  // -5 to +5
		float randomY = (float)(_random.NextDouble() * 5);        // 0 to +5
		Position = position + new Vector2(randomX, randomY);

		// Float upward with slight horizontal arc
		float horizontalDrift = (float)((_random.NextDouble() * 20) - 10);  // -10 to +10
		_velocity = new Vector2(horizontalDrift, -80f);  // Faster upward movement

		// Color based on type or custom
		if (customColor.HasValue) {
			_color = customColor.Value;
		} else if (isCrit) {
			_color = Color.Yellow;  // Crits are yellow
		} else {
			_color = Color.White;   // Normal damage is white
		}

		// Scale based on crit AND damage amount
		if (isCrit) {
			_scale = 3f;  // Crits are BIG!
		} else if (damage >= 100) {
			_scale = 2.5f;
		} else if (damage >= 50) {
			_scale = 2f;
		} else {
			_scale = 1.5f;  // Even normal hits are slightly bigger
		}
	}

	public void Update(GameTime gameTime) {
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		_timer += deltaTime;

		// Move upward
		Position += _velocity * deltaTime;

		// Slow down over time
		_velocity *= 0.95f;

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

		// Add pop effect at start (scale up briefly)
		float popScale = _scale;
		if (_timer < 0.1f) {
			popScale = _scale * (1f + ((0.1f - _timer) * 2f));  // Brief scale up
		}

		Color drawColor = _color * alpha;
		string text = Damage.ToString();

		// Draw with shadow for readability
		Color shadowColor = Color.Black * alpha;
		_font.DrawText(spriteBatch, text, Position + new Vector2(1, 1), shadowColor, (int)popScale);
		_font.DrawText(spriteBatch, text, Position, drawColor, (int)popScale);
	}
}