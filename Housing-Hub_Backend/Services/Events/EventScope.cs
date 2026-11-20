using System.Security.Claims;

namespace HousingHubBackend.Services.Events
{
    public sealed record EventScope(int? UserId, int? FlatId, int? SocietyId, string? Role)
    {
        public static EventScope FromClaims(ClaimsPrincipal user)
        {
            static int? Parse(string? value)
            {
                return int.TryParse(value, out var parsed) ? parsed : null;
            }

            return new EventScope(
                Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)),
                Parse(user.FindFirstValue("FlatId")),
                Parse(user.FindFirstValue("SocietyId")),
                user.FindFirstValue(ClaimTypes.Role));
        }
    }
}
