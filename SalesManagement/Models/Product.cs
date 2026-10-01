namespace SalesManagement.Models;

public class Product : IHasCreatedAt
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public int WarrantyPeriod { get; set; }

    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<WarehouseItem> WarehouseItems { get; set; } = new List<WarehouseItem>();
}
