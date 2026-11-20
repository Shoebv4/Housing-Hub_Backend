using HousingHubBackend.Services.Events;

namespace HousingHubBackend.Services.Interfaces
{
    public interface IEventBroker
    {
        EventSubscription Subscribe(EventScope scope);
        void Publish(RealtimeEvent message);
    }
}
