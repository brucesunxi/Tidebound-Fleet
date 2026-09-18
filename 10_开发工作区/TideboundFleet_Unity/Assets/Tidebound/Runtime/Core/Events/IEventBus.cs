using System;

namespace Tidebound.Events
{
    public interface ITideboundEvent { }

    /// <summary>One main-thread bus per session. Dispose subscriptions or the whole bus on teardown.</summary>
    public interface IEventBus : IDisposable
    {
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, ITideboundEvent;
        void Publish<T>(T message) where T : struct, ITideboundEvent;
    }
}
