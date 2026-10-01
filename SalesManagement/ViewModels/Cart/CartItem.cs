namespace SalesManagement.ViewModels;

public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }

    public decimal SubTotal => Price * Quantity;
}
