using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HousingHubBackend.Services.Events
{
    public sealed class RealtimeEvent
    {
        public required string Type { get; init; }
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
        public object? Data { get; init; }

        [JsonIgnore]
        public IReadOnlyCollection<int> UserIds { get; init; } = Array.Empty<int>();

        [JsonIgnore]
        public IReadOnlyCollection<int> FlatIds { get; init; } = Array.Empty<int>();

        [JsonIgnore]
        public int? SocietyId { get; init; }

        [JsonIgnore]
        public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

        public bool IsTargetedAt(EventScope scope)
        {
            if (scope.UserId is int userId && UserIds.Contains(userId))
            {
                return true;
            }

            if (scope.FlatId is int flatId && FlatIds.Contains(flatId))
            {
                return true;
            }

            if (SocietyId is int societyId && scope.SocietyId == societyId)
            {
                return Roles.Count == 0 || (scope.Role is not null && Roles.Contains(scope.Role));
            }

            return false;
        }
    }
}
