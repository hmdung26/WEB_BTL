namespace SalesManagement.ViewModels;

public class ProductReviewItemViewModel
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string UserName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
