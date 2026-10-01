namespace SalesManagement.ViewModels;

public class CategorySectionViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public List<ProductListItemViewModel> Products { get; set; } = new();
}
