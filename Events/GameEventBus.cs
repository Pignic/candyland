using System;
using System.Collections.Generic;
using System.Threading;

namespace EldmeresTale.Events;

public class GameEventBus : IDisposable {

	private readonly Dictionary<Type, List<Delegate>> _subscribers = [];
	private readonly Lock _lock = new Lock();

	// ===== SUBSCRIBE =====

	public EventSubscription Subscribe<T>(Action<T> handler) where T : GameEvent {
		lock (_lock) {
			Type eventType = typeof(T);

			if (!_subscribers.TryGetValue(eventType, out List<Delegate> value)) {
				value = [];
				_subscribers[eventType] = value;
			}

			value.Add(handler);

			System.Diagnostics.Debug.WriteLine($"[EVENT BUS] Subscribed to {typeof(T).Name}");

			// Return token for unsubscribing
			return new EventSubscription(() => Unsubscribe(handler));
		}
	}

	public void Unsubscribe<T>(Action<T> handler) where T : GameEvent {
		lock (_lock) {
			Type eventType = typeof(T);

			if (_subscribers.TryGetValue(eventType, out List<Delegate> handlers)) {
				handlers.Remove(handler);

				if (handlers.Count == 0) {
					_subscribers.Remove(eventType);
				}

				System.Diagnostics.Debug.WriteLine($"[EVENT BUS] Unsubscribed from {typeof(T).Name}");
			}
		}
	}

	// ===== PUBLISH =====

	public void Publish<T>(T gameEvent) where T : GameEvent {
		List<Delegate> handlersCopy;

		lock (_lock) {
			Type eventType = typeof(T);

			if (!_subscribers.TryGetValue(eventType, out List<Delegate> handlers)) {
				return; // No subscribers
			}

			// Copy to avoid modification during iteration
			handlersCopy = [.. handlers];
		}

		// Invoke outside lock to avoid deadlocks
		foreach (Delegate handler in handlersCopy) {
			try {
				((Action<T>)handler)(gameEvent);
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine($"[EVENT BUS] Error in event handler for {typeof(T).Name}: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"[EVENT BUS] Stack trace: {ex.StackTrace}");
			}
		}
	}

	// ===== UTILITY =====

	public int GetSubscriberCount<T>() where T : GameEvent {
		lock (_lock) {
			Type eventType = typeof(T);
			if (_subscribers.TryGetValue(eventType, out List<Delegate> handlers)) {
				return handlers.Count;
			}
			return 0;
		}
	}

	public void Clear() {
		lock (_lock) {
			_subscribers.Clear();
			System.Diagnostics.Debug.WriteLine("[EVENT BUS] Cleared all subscriptions");
		}
	}

	// ===== DISPOSE =====

	public void Dispose() {
		lock (_lock) {
			int totalSubscribers = 0;
			foreach (KeyValuePair<Type, List<Delegate>> kvp in _subscribers) {
				totalSubscribers += kvp.Value.Count;
			}

			_subscribers.Clear();
			System.Diagnostics.Debug.WriteLine($"[EVENT BUS] Disposed ({totalSubscribers} subscriptions cleared)");
		}
	}
}
