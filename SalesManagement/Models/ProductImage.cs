namespace SalesManagement.Models;

public class ProductImage
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = null!;
    public int SortOrder { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
