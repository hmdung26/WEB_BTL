using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Services.Interfaces;

namespace SalesManagement.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(int userId, string title, string? message)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Read = false
        });

        await _context.SaveChangesAsync();
    }
}
