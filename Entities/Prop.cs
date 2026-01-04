using EldmoresTale.World;
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
	public Texture2D texture { get; set; }

	public Color tint { get; set; } = Color.White;

	public bool isCollidable { get; set; } = true;
	public bool isActive { get; set; } = true;

	// Interaction
	public PropType type { get; set; }
	public string interactionText { get; set; } = "Press E";
	public Action<Prop> onInteract { get; set; }  // Called when player presses E
	public Action<Prop> onBreak { get; set; }     // Called when prop is destroyed

	public bool isBroken => health <= 0;

	// Pushable props
	public bool isPushable => type == PropType.Pushable;
	public float pushSpeed { get; set; } = 50f;
	public Vector2 pushVelocity { get; set; } = Vector2.Zero;
	private float friction = 0.9f;

	// Animation (optional)
	private float animationTimer = 0f;
	private float shakeAmount = 0f;

	// Loot drops (for breakables)
	public string[] lootTable { get; set; }  // Item IDs to potentially drop
	public float lootChance { get; set; } = 0.5f;

	public Prop(Texture2D texture, Vector2 position, PropType type, int width = 16, int height = 16, float speed = 0) : base(texture, position, width, height, speed) {
		this.texture = texture;
		Position = position;
		this.type = type;
		Width = width;
		Height = height;

		// Set defaults based on type
		switch (type) {
			case PropType.Breakable:
				health = 3;
				MaxHealth = 3;
				isCollidable = true;
				break;
			case PropType.Pushable:
				isCollidable = true;
				break;
			case PropType.Interactive:
				isCollidable = true;
				break;
			case PropType.Collectible:
				isCollidable = false;
				break;
			case PropType.Static:
				isCollidable = true;
				break;
		}
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		if (!isActive) {
			return;
		}

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		// Update pushable movement
		if (isPushable && pushVelocity != Vector2.Zero) {
			Position += pushVelocity * deltaTime;
			pushVelocity *= friction;

			// Stop if moving very slowly
			if (pushVelocity.Length() < 5f) {
				pushVelocity = Vector2.Zero;
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
		if (!isActive) {
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
		Color drawColor = tint;
		if (type == PropType.Breakable && health < MaxHealth && animationTimer % 0.2f < 0.1f) {
			drawColor = Color.Lerp(tint, Color.Red, 0.5f);
		}

		spriteBatch.Draw(texture, destRect, drawColor);
	}

	public void Interact() {
		if (!isActive) {
			return;
		}

		onInteract?.Invoke(this);

		// Default interactions
		if (type == PropType.Interactive) {
			System.Diagnostics.Debug.WriteLine($"Interacted with prop at {Position}");
		}
	}

	public void TakeDamage(int damage) {
		if (type != PropType.Breakable || !isActive) {
			return;
		}

		health -= damage;
		shakeAmount = 5f;

		if (health <= 0) {
			Break();
		}
	}

	private void Break() {
		isActive = false;
		onBreak?.Invoke(this);

		System.Diagnostics.Debug.WriteLine($"Prop broken at {Position}");
	}

	public void Push(Vector2 direction, float force = 100f) {
		if (!isPushable || !isActive) {
			return;
		}

		direction.Normalize();
		pushVelocity = direction * force;
	}

	public bool IsPlayerInRange(Vector2 playerPosition, float range = 32f) {
		Vector2 propCenter = Position + new Vector2(Width / 2f, Height / 2f);
		float distance = Vector2.Distance(playerPosition, propCenter);
		return distance <= range;
	}

	public void ApplyWorldBounds(Rectangle worldBounds) {
		if (!isPushable) {
			return;
		}

		Position = new Vector2(
			MathHelper.Clamp(Position.X, worldBounds.X, worldBounds.Right - Width),
			MathHelper.Clamp(Position.Y, worldBounds.Y, worldBounds.Bottom - Height)
		);
	}

	public bool CheckTileCollision(World.TileMap map) {
		if (!isPushable) {
			return false;
		}

		// Check tile collision at prop position
		string tileType = map.GetTileAtPosition(Position + new Vector2(Width / 2, Height / 2));
		return !TileRegistry.Instance.GetTile(tileType).IsWalkable;
	}
}