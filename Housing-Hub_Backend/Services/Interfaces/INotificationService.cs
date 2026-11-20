using System.Threading.Tasks;

namespace HousingHubBackend.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string to, string subject, string message);
    }
}