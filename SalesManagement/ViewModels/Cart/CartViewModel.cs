namespace SalesManagement.ViewModels;

public class CartViewModel
{
    public List<CartItem> Items { get; set; } = new();
    public decimal SubTotal => Items.Sum(i => i.SubTotal);
    public decimal DiscountAmount { get; set; }
    public decimal Total => SubTotal - DiscountAmount;
    public string? PromoCode { get; set; }
    public bool PromoApplied { get; set; }
    public string? PromoMessage { get; set; }
}
