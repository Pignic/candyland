
using Candyland.Core;
using Candyland.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class SceneManager: IDisposable {

	private readonly Stack<Scene> _stack = new();
	private readonly Queue<Action> _pending = new();
	private readonly ApplicationContext appContext;

	public SceneManager(ApplicationContext appContext) {
		this.appContext = appContext;
	}

	public void Push(Scene scene) {
		_pending.Enqueue(() => {
			scene.Load();
			_stack.Push(scene);
		});
	}

	public void Pop() {
		_pending.Enqueue(() => {
			var top = _stack.Pop();
			top.Dispose();
		});
	}

	public void Replace(Scene scene) {
		_pending.Enqueue(() => {
			while(_stack.Count > 0) {
				var s = _stack.Pop();
				s.Dispose();
			}
			scene.Load();
			_stack.Push(scene);
		});
	}

	public void Update(GameTime gameTime) {
		ApplyPending();
		foreach(var scene in _stack) {
			scene.Update(gameTime);
			if(scene.exclusive) {
				break;
			}
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		ApplyPending();
		foreach(var scene in _stack.Reverse()) {
			scene.Draw(spriteBatch);
			if(scene.exclusive) {
				break;
			}
		}
	}

	private void ApplyPending() {
		while(_pending.Count > 0) {
			_pending.Dequeue().Invoke();
		}
	}

	public void Dispose () {
		foreach(var scene in _stack) {
			scene.Dispose();
		}
	}
}