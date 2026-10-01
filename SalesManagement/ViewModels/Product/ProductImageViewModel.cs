namespace SalesManagement.ViewModels;

public class ProductImageViewModel
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = null!;
    public int SortOrder { get; set; }
}
