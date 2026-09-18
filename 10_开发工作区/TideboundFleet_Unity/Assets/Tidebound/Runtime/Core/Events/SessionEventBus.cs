using System;
using System.Collections.Generic;

namespace Tidebound.Events
{
    /// <summary>Synchronous, ordered, fail-fast dispatch. Each publication uses a subscription snapshot.</summary>
    public sealed class SessionEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Subscription>> listeners = new Dictionary<Type, List<Subscription>>();
        private bool disposed;

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, ITideboundEvent
        {
            ThrowIfDisposed();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(T);
            if (!listeners.TryGetValue(type, out var list)) listeners.Add(type, list = new List<Subscription>());
            var subscription = new Subscription(handler);
            // Each registration gets its own identity, including duplicate registrations of one delegate.
            subscription.Unsubscribe = () => list.Remove(subscription);
            list.Add(subscription);
            return subscription;
        }

        public void Publish<T>(T message) where T : struct, ITideboundEvent
        {
            ThrowIfDisposed();
            if (!listeners.TryGetValue(typeof(T), out var list)) return;
            foreach (var subscription in list.ToArray()) ((Action<T>)subscription.Handler)(message);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var list in listeners.Values)
            {
                foreach (var subscription in list) subscription.Unsubscribe = null;
                list.Clear();
            }
            listeners.Clear();
        }

        private void ThrowIfDisposed()
        { if (disposed) throw new ObjectDisposedException(nameof(SessionEventBus)); }

        private sealed class Subscription : IDisposable
        {
            public Action Unsubscribe;
            public Delegate Handler { get; }
            public Subscription(Delegate handler) { Handler = handler; }
            public void Dispose() { var action = Unsubscribe; Unsubscribe = null; action?.Invoke(); }
        }
    }
}
