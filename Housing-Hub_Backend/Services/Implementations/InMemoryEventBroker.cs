using System.Collections.Concurrent;
using HousingHubBackend.Services.Events;
using HousingHubBackend.Services.Interfaces;

namespace HousingHubBackend.Services.Implementations
{
    public sealed class InMemoryEventBroker : IEventBroker
    {
        private const int ChannelCapacity = 128;
        private readonly ConcurrentDictionary<Guid, EventSubscription> _subscriptions = new();

        public EventSubscription Subscribe(EventScope scope)
        {
            var subscription = new EventSubscription(scope, ChannelCapacity, Remove);
            _subscriptions[subscription.Id] = subscription;
            return subscription;
        }

        public void Publish(RealtimeEvent message)
        {
            foreach (var subscription in _subscriptions.Values)
            {
                if (!message.IsTargetedAt(subscription.Scope))
                {
                    continue;
                }

                if (!subscription.Writer.TryWrite(message))
                {
                    subscription.Dispose();
                }
            }
        }

        private void Remove(Guid id)
        {
            _subscriptions.TryRemove(id, out _);
        }
    }
}
