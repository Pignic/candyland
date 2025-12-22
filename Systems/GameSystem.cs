using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.Systems;

public abstract class GameSystem : IDisposable {

	public bool Enabled { get; set; }
	public bool Visible { get; set; }

	public virtual void Initialize() {

	}

	public virtual void Update(GameTime time) {

	}

	public virtual void Draw(SpriteBatch spriteBatch) {

	}

	public virtual void Dispose() {

	}
}