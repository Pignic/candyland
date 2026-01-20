using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Entities;

public abstract class ActorEntity : BaseEntity {

	public Vector2 PreviousPosition { get; set; }

	public bool IsDying { get; protected set; } = false;

	public event Action<ActorEntity> OnAttack;
	public event Action<ActorEntity> OnAttacked;

	protected ActorEntity(Texture2D texture, Vector2 position, int width, int height, float speed) : base(texture, position, width, height, speed) {
		PreviousPosition = new Vector2(base.Position.X, base.Position.Y);
	}

	protected ActorEntity(Texture2D spriteSheet, Vector2 position, int frameCount, int frameWidth, int frameHeight, float frameTime, int width, int height, float speed, bool pingpong = false) : base(spriteSheet, position, frameCount, frameWidth, frameHeight, frameTime, width, height, speed, pingpong) {
		PreviousPosition = new Vector2(position.X, position.Y);
	}

	protected void InvokeAttackEvent() {
		OnAttack?.Invoke(this);
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		PreviousPosition = new Vector2(base.Position.X, base.Position.Y);
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
	}
}
