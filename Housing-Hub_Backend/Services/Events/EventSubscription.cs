using System.Threading;
using System.Threading.Channels;

namespace HousingHubBackend.Services.Events
{
    public sealed class EventSubscription : IDisposable
    {
        private readonly Action<Guid> _onDispose;
        private int _disposed;

        public EventSubscription(EventScope scope, int capacity, Action<Guid> onDispose)
        {
            Id = Guid.NewGuid();
            Scope = scope;
            _onDispose = onDispose;

            var channel = Channel.CreateBounded<RealtimeEvent>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });

            Writer = channel.Writer;
            Reader = channel.Reader;
        }

        public Guid Id { get; }
        public EventScope Scope { get; }
        public ChannelWriter<RealtimeEvent> Writer { get; }
        public ChannelReader<RealtimeEvent> Reader { get; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            Writer.TryComplete();
            _onDispose(Id);
        }
    }
}
