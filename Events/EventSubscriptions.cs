using System;
using System.Collections.Generic;

namespace EldmeresTale.Events;

public class EventSubscription : IDisposable {
	private readonly Action _unsubscribe;
	private bool _disposed;

	public EventSubscription(Action unsubscribe) {
		_unsubscribe = unsubscribe;
	}

	public void Dispose() {
		if (!_disposed) {
			_unsubscribe?.Invoke();
			_disposed = true;
		}
	}
}

public class EventSubscriptions : IDisposable {
	private readonly List<EventSubscription> _subscriptions = [];

	public void Add(EventSubscription subscription) {
		_subscriptions.Add(subscription);
	}

	public void Dispose() {
		foreach (EventSubscription subscription in _subscriptions) {
			subscription?.Dispose();
		}
		_subscriptions.Clear();

		System.Diagnostics.Debug.WriteLine($"[EVENT SUBSCRIPTIONS] Disposed {_subscriptions.Count} subscriptions");
	}

	public int Count => _subscriptions.Count;
}
