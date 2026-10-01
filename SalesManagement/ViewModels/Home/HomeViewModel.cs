using SalesManagement.Models;

namespace SalesManagement.ViewModels;

public class HomeViewModel
{
    public List<Banner> Banners { get; set; } = new();
    public List<ProductListItemViewModel> NewestProducts { get; set; } = new();
    public List<ProductListItemViewModel> TopRatedProducts { get; set; } = new();
    public List<CategorySectionViewModel> CategorySections { get; set; } = new();
}
