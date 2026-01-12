using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Entities;

public enum PickupType {
	HealthPotion,
	Coin,
	BigCoin
}

public class Pickup {
	public Vector2 Position { get; set; }
	public PickupType Type { get; }
	public bool IsCollected { get; set; }

	private readonly Texture2D _texture;
	private readonly int _size = 16;
	private readonly float _bobSpeed = 3f;
	private readonly float _bobAmount = 3f;
	private readonly float _baseY;

	private float _bobTimer = 0f;

	// TODO: implement types of pickups
	public string ItemId { get; set; } = "";

	// Pickup values
	public int HealthRestore { get; }
	public int CoinValue { get; }

	public Rectangle Bounds => new Rectangle(
		(int)Position.X,
		(int)Position.Y,
		_size,
		_size
	);

	public Pickup(PickupType type, Vector2 position, Texture2D texture) {
		Type = type;
		Position = position;
		_texture = texture;
		_baseY = position.Y;
		IsCollected = false;

		// Set values based on type
		switch (type) {
			case PickupType.HealthPotion:
				HealthRestore = 25;
				CoinValue = 0;
				_size = 16;
				break;
			case PickupType.Coin:
				HealthRestore = 0;
				CoinValue = 1;
				_size = 12;
				break;
			case PickupType.BigCoin:
				HealthRestore = 0;
				CoinValue = 5;
				_size = 16;
				break;
		}
	}

	public void Update(GameTime gameTime) {
		if (IsCollected) {
			return;
		}
		// Bob up and down animation
		_bobTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * _bobSpeed;
		Position = new Vector2(Position.X, _baseY + ((float)Math.Sin(_bobTimer) * _bobAmount));
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (IsCollected) {
			return;
		}
		spriteBatch.Draw(_texture, Position, Color.White);
	}

	public bool CheckCollision(BaseEntity entity) {
		if (IsCollected) {
			return false;
		}
		return Bounds.Intersects(entity.Bounds);
	}

	public void Collect() {
		IsCollected = true;
	}
}