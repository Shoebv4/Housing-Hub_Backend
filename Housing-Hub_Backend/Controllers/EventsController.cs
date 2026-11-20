using System.Text.Json;
using System.Threading.Channels;
using HousingHubBackend.Services.Events;
using HousingHubBackend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace HousingHubBackend.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(20);

        private readonly IEventBroker _eventBroker;

        public EventsController(IEventBroker eventBroker)
        {
            _eventBroker = eventBroker;
        }

        [HttpGet("stream")]
        public async Task Stream(CancellationToken cancellationToken)
        {
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache, no-store, no-transform";
            Response.Headers["X-Accel-Buffering"] = "no";

            using var subscription = _eventBroker.Subscribe(EventScope.FromClaims(User));
            await WriteAsync("retry: 3000\n\n", cancellationToken);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    heartbeatCts.CancelAfter(HeartbeatInterval);

                    try
                    {
                        var message = await subscription.Reader.ReadAsync(heartbeatCts.Token);
                        var json = JsonSerializer.Serialize(message, JsonOptions);
                        await WriteAsync($"event: {message.Type}\ndata: {json}\n\n", cancellationToken);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        await WriteAsync(": heartbeat\n\n", cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected.
            }
            catch (ChannelClosedException)
            {
                // The broker closed this subscription for a slow consumer or shutdown.
            }
            catch (IOException)
            {
                // Client disconnected while writing.
            }
        }

        private async Task WriteAsync(string text, CancellationToken cancellationToken)
        {
            await Response.WriteAsync(text, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
