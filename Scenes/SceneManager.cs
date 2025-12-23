using Candyland.Core;
using Candyland.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class SceneManager : IDisposable {

	private readonly Stack<Scene> _stack = new();
	private readonly Queue<Action> _pending = new();
	private readonly ApplicationContext appContext;

	public SceneManager(ApplicationContext appContext) {
		this.appContext = appContext;
	}

	public void Push(Scene scene) {
		_pending.Enqueue(() => {
			System.Diagnostics.Debug.WriteLine($"Push scene: {scene.GetType().Name}");
			scene.Load();
			_stack.Push(scene);
		});
	}

	public void Pop() {
		_pending.Enqueue(() => {
			Scene top = _stack.Pop();
			System.Diagnostics.Debug.WriteLine($"Pop scene: {top.GetType().Name}");
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
		foreach(var scene in _stack.Reverse()) {
			scene.Draw(spriteBatch);
		}
	}

	private bool ApplyPending() {
		bool pendingApplied = false;
		while(_pending.Count > 0) {
			_pending.Dequeue().Invoke();
			pendingApplied = true;
		}
		return pendingApplied;
	}

	public void Dispose() {
		foreach(var scene in _stack) {
			scene.Dispose();
		}
	}
}