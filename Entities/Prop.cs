using EldmeresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Entities;

public enum PropType {
	Static,         // Just an obstacle (rock, tree stump)
	Breakable,      // Can be destroyed (crate, barrel, pot)
	Pushable,       // Can be pushed around (box, boulder)
	Interactive,    // Press E to interact (chest, lever, sign)
	Collectible     // Auto-collect on touch (coin, heart)
}

public class Prop : Entity {
	// Visual
	public Texture2D Texture { get; set; }

	public Color Tint { get; set; } = Color.White;

	public bool IsCollidable { get; set; } = true;
	public bool IsActive { get; set; } = true;

	// Interaction
	public PropType Type { get; set; }
	public string InteractionText { get; set; } = "Press E";
	public Action<Prop> OnInteract { get; set; }  // Called when player presses E
	public Action<Prop> OnBreak { get; set; }     // Called when prop is destroyed

	public bool IsBroken => Health <= 0;

	// Pushable props
	public bool IsPushable => Type == PropType.Pushable;
	public float PushSpeed { get; set; } = 50f;
	public Vector2 PushVelocity { get; set; } = Vector2.Zero;
	private readonly float friction = 0.9f;

	// Animation (optional)
	private float animationTimer = 0f;
	private float shakeAmount = 0f;

	// Loot drops (for breakables)
	public string[] LootTable { get; set; }  // Item IDs to potentially drop
	public float LootChance { get; set; } = 0.5f;

	public Prop(Texture2D texture, Vector2 position, PropType type, int width = 16, int height = 16, float speed = 0) : base(texture, position, width, height, speed) {
		Texture = texture;
		Position = position;
		Type = type;
		Width = width;
		Height = height;

		// Set defaults based on type
		switch (type) {
			case PropType.Breakable:
				Health = 3;
				MaxHealth = 3;
				IsCollidable = true;
				break;
			case PropType.Pushable:
				IsCollidable = true;
				break;
			case PropType.Interactive:
				IsCollidable = true;
				break;
			case PropType.Collectible:
				IsCollidable = false;
				break;
			case PropType.Static:
				IsCollidable = true;
				break;
		}
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		if (!IsActive) {
			return;
		}

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		// Update pushable movement
		if (IsPushable && PushVelocity != Vector2.Zero) {
			Position += PushVelocity * deltaTime;
			PushVelocity *= friction;

			// Stop if moving very slowly
			if (PushVelocity.Length() < 5f) {
				PushVelocity = Vector2.Zero;
			}
		}

		// Update animations
		animationTimer += deltaTime;

		// Reduce shake over time
		if (shakeAmount > 0) {
			shakeAmount *= 0.9f;
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		//base.Draw(spriteBatch);
		if (!IsActive) {
			return;
		}

		// Apply shake effect
		Vector2 drawPos = Position;
		if (shakeAmount > 0) {
			Random random = new Random();
			drawPos.X += (float)(random.NextDouble() - 0.5) * shakeAmount;
			drawPos.Y += (float)(random.NextDouble() - 0.5) * shakeAmount;
		}

		Rectangle destRect = new Rectangle((int)drawPos.X, (int)drawPos.Y, Width, Height);

		// Flash red when damaged
		Color drawColor = Tint;
		if (Type == PropType.Breakable && Health < MaxHealth && animationTimer % 0.2f < 0.1f) {
			drawColor = Color.Lerp(Tint, Color.Red, 0.5f);
		}

		spriteBatch.Draw(Texture, destRect, drawColor);
	}

	public void Interact() {
		if (!IsActive) {
			return;
		}

		OnInteract?.Invoke(this);

		// Default interactions
		if (Type == PropType.Interactive) {
			System.Diagnostics.Debug.WriteLine($"Interacted with prop at {Position}");
		}
	}

	public void TakeDamage(int damage) {
		if (Type != PropType.Breakable || !IsActive) {
			return;
		}

		Health -= damage;
		shakeAmount = 5f;

		if (Health <= 0) {
			Break();
		}
	}

	private void Break() {
		IsActive = false;
		OnBreak?.Invoke(this);

		System.Diagnostics.Debug.WriteLine($"Prop broken at {Position}");
	}

	public void Push(Vector2 direction, float force = 100f) {
		if (!IsPushable || !IsActive) {
			return;
		}

		direction.Normalize();
		PushVelocity = direction * force;
	}

	public bool IsPlayerInRange(Vector2 playerPosition, float range = 32f) {
		Vector2 propCenter = Position + new Vector2(Width / 2f, Height / 2f);
		float distance = Vector2.Distance(playerPosition, propCenter);
		return distance <= range;
	}

	public void ApplyWorldBounds(Rectangle worldBounds) {
		if (!IsPushable) {
			return;
		}

		Position = new Vector2(
			MathHelper.Clamp(Position.X, worldBounds.X, worldBounds.Right - Width),
			MathHelper.Clamp(Position.Y, worldBounds.Y, worldBounds.Bottom - Height)
		);
	}

	public bool CheckTileCollision(World.TileMap map) {
		if (!IsPushable) {
			return false;
		}

		// Check tile collision at prop position
		string tileType = map.GetTileAtPosition(Position + new Vector2(Width / 2, Height / 2));
		return !TileRegistry.Instance.GetTile(tileType).IsWalkable;
	}
}