using EldmeresTale.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Scenes;

public abstract class Scene : IDisposable {

	protected readonly ApplicationContext appContext;

	protected Camera camera;

	public bool Exclusive { get; set; }


	protected Scene(ApplicationContext appContext, bool exclusive = true) {
		this.appContext = appContext;
		Exclusive = exclusive;
		this.appContext.Display.DisplayChanged += OnDisplayChanged;
	}

	public virtual void Update(GameTime time) {

	}

	public virtual void Draw(SpriteBatch spriteBatch) {

	}

	public virtual void Load() {

	}

	public virtual void Dispose() {
		appContext.Display.DisplayChanged -= OnDisplayChanged;
	}

	public virtual void OnDisplayChanged() {
		camera?.SetSize(
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight
		);
	}

	public Camera GetCamera() {
		return camera;
	}
}
