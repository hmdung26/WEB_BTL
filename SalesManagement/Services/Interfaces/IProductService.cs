using SalesManagement.ViewModels;

namespace SalesManagement.Services.Interfaces;

public interface IProductService
{
    Task<ProductListViewModel> GetProductsAsync(ProductFilter filter);

    Task<ProductDetailsViewModel?> GetDetailsAsync(int id, int? currentUserId);

    Task<List<ProductListItemViewModel>> GetNewestAsync(int count);

    Task<List<ProductListItemViewModel>> GetTopRatedAsync(int count);

    Task<List<CategorySectionViewModel>> GetProductsByCategoryAsync(int perCategory);
}
