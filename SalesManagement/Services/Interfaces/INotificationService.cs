namespace SalesManagement.Services.Interfaces;

public interface INotificationService
{
    Task CreateAsync(int userId, string title, string? message);
}
