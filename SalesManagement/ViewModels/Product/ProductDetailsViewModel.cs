namespace SalesManagement.ViewModels;

public class ProductDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public int WarrantyPeriod { get; set; }
    public string BrandName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public List<string> ImageUrls { get; set; } = new();
    public List<ProductReviewItemViewModel> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool CanReview { get; set; }
}
