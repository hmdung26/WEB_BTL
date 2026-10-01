using Microsoft.AspNetCore.Mvc.Rendering;

namespace SalesManagement.ViewModels;

public class ProductListViewModel
{
    public List<ProductListItemViewModel> Items { get; set; } = new();
    public ProductFilter Filter { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();
    public List<SelectListItem> BrandOptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
