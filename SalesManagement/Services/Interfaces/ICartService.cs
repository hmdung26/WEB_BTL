using SalesManagement.ViewModels;

namespace SalesManagement.Services.Interfaces;

public interface ICartService
{
    Task<CartViewModel> GetCartAsync();

    Task<CartItem?> GetItemAsync(int productId);

    Task AddItemAsync(int productId, int quantity);

    Task UpdateQuantityAsync(int productId, int quantity);

    Task RemoveItemAsync(int productId);

    Task ClearAsync();

    Task ApplyPromotionAsync(string code);

    Task ClearPromotionAsync();
}
