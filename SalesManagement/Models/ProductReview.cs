namespace SalesManagement.Models;

public class ProductReview : IHasCreatedAt
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
