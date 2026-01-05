using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class SystemManager : IDisposable {

	private List<GameSystem> _systems = new();

	public void Initialize() {
		foreach (GameSystem system in _systems) {
			system.Initialize();
		}
	}

	public void AddSystem(GameSystem system) {
		_systems.Add(system);
	}

	public void Update(GameTime time) {
		foreach (GameSystem system in _systems) {
			system.Update(time);
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		foreach (GameSystem system in _systems) {
			system.Draw(spriteBatch);
		}
	}

	public void Dispose() {
		foreach (GameSystem system in _systems) {
			system.Dispose();
		}
	}
}