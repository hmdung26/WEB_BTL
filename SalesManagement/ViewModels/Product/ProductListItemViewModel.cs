namespace SalesManagement.ViewModels;

public class ProductListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string BrandName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public int StockQuantity { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
}
