namespace SalesManagement.Models;

public class Banner : IHasCreatedAt
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public bool Active { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}
