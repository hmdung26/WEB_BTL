namespace SalesManagement.Models;

public class Notification : IHasCreatedAt
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Message { get; set; }
    public bool Read { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
