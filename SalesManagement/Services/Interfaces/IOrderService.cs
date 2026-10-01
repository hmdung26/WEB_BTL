using SalesManagement.Models;

namespace SalesManagement.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Creates an order from the current user's cart, applies the promotion (if valid),
    /// deducts stock (Product.StockQuantity + WarehouseItem records), and returns the new order.
    /// Throws <see cref="InvalidOperationException"/> on business-rule violations.
    /// </summary>
    Task<SalesOrder> CreateOrderFromCartAsync(ApplicationUser user, string customerName, string? customerPhone, string? customerAddress);

    /// <summary>
    /// Cancels an order that is still Pending/Processing: restores stock and frees reserved warehouse items.
    /// </summary>
    Task CancelOrderAsync(int orderId, int userId);
}
