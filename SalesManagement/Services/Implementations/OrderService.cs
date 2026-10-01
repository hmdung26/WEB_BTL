using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.Services.Interfaces;

namespace SalesManagement.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ICartService _cartService;
    private readonly INotificationService _notificationService;

    public OrderService(AppDbContext context, ICartService cartService, INotificationService notificationService)
    {
        _context = context;
        _cartService = cartService;
        _notificationService = notificationService;
    }

    public async Task<SalesOrder> CreateOrderFromCartAsync(
        ApplicationUser user, string customerName, string? customerPhone, string? customerAddress)
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException("Giỏ hàng trống.");
        }

        // Load products and lock them for the duration of the transaction.
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var item in cart.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            if (product.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Sản phẩm \"{product.Name}\" không đủ số lượng tồn kho (còn {product.StockQuantity}).");
            }
        }

        // Determine discount from the applied promotion.
        var promotion = await ResolvePromotionAsync(cart.PromoCode, cart.SubTotal);
        var discount = promotion is null ? 0m : CalculateDiscount(promotion, cart.SubTotal);

        var order = new SalesOrder
        {
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            CustomerAddress = customerAddress,
            SubTotal = cart.SubTotal,
            DiscountAmount = discount,
            TotalAmount = cart.SubTotal - discount,
            Status = OrderStatus.Pending,
            UserId = user.Id,
            PromotionId = promotion?.Id,
            Payment = new Payment
            {
                Method = PaymentMethod.Cod,
                Status = PaymentStatus.Pending,
                Amount = cart.SubTotal - discount
            },
            OrderItems = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price,
                SubTotal = i.SubTotal
            }).ToList()
        };

        _context.Orders.Add(order);

        // Deduct aggregate stock.
        foreach (var item in cart.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.StockQuantity -= item.Quantity;
        }

        // Reserve per-serial warehouse items to keep both inventory layers in sync.
        foreach (var item in cart.Items)
        {
            await ReserveWarehouseItemsAsync(item.ProductId, item.Quantity, order);
        }

        // Increment promotion usage.
        if (promotion is not null)
        {
            promotion.UsedCount++;
        }

        await _context.SaveChangesAsync();
        await _cartService.ClearAsync();

        await _notificationService.CreateAsync(user.Id, "Đặt hàng thành công",
            $"Đơn hàng #{order.Id} của bạn đã được ghi nhận với tổng {order.TotalAmount:N0} đ.");

        return order;
    }

    public async Task CancelOrderAsync(int orderId, int userId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order is null)
        {
            throw new InvalidOperationException("Không tìm thấy đơn hàng.");
        }

        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Đơn hàng không thể hủy ở trạng thái hiện tại.");
        }

        // Restore aggregate stock.
        foreach (var item in order.OrderItems)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product is not null)
            {
                product.StockQuantity += item.Quantity;
            }
        }

        // Release reserved warehouse items.
        var reservedItems = await _context.WarehouseItems
            .Where(w => w.OrderId == order.Id && w.Status == WarehouseItemStatus.Reserved)
            .ToListAsync();

        foreach (var wi in reservedItems)
        {
            wi.Status = WarehouseItemStatus.Available;
            wi.OrderId = null;
            wi.LastUpdated = DateTime.UtcNow;
        }

        order.Status = OrderStatus.Cancelled;

        // Release the promotion slot if one was used.
        if (order.PromotionId.HasValue)
        {
            var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == order.PromotionId.Value);
            if (promo is not null && promo.UsedCount > 0)
            {
                promo.UsedCount--;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ReserveWarehouseItemsAsync(int productId, int quantity, SalesOrder order)
    {
        var available = await _context.WarehouseItems
            .Where(w => w.ProductId == productId && w.Status == WarehouseItemStatus.Available)
            .OrderBy(w => w.Id)
            .Take(quantity)
            .ToListAsync();

        var reservedCount = 0;
        foreach (var wi in available)
        {
            wi.Status = WarehouseItemStatus.Reserved;
            wi.ReservedOrder = order;
            wi.LastUpdated = DateTime.UtcNow;
            reservedCount++;
        }

        // If there aren't enough serialized records, create the remainder so the two
        // inventory layers stay consistent with the aggregate stock deduction.
        for (var i = reservedCount; i < quantity; i++)
        {
            _context.WarehouseItems.Add(new WarehouseItem
            {
                ProductId = productId,
                Barcode = GenerateCode("BC"),
                SerialNumber = GenerateCode("SN"),
                Status = WarehouseItemStatus.Reserved,
                ReservedOrder = order,
                LastUpdated = DateTime.UtcNow
            });
        }
    }

    private async Task<Models.Promotion?> ResolvePromotionAsync(string? code, decimal subTotal)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var promo = await _context.Promotions
            .FirstOrDefaultAsync(p => p.Code == code && p.Active);

        if (promo is null)
        {
            return null;
        }

        if (now < promo.StartAt || now > promo.EndAt)
        {
            return null;
        }

        if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
        {
            return null;
        }

        if (subTotal < promo.MinOrderValue)
        {
            return null;
        }

        return promo;
    }

    private static decimal CalculateDiscount(Models.Promotion promo, decimal subTotal)
    {
        return promo.DiscountType == PromotionDiscountType.Percent
            ? Math.Round(subTotal * promo.DiscountValue / 100m, 2)
            : Math.Min(promo.DiscountValue, subTotal);
    }

    private static string GenerateCode(string prefix)
    {
        return $"{prefix}-{DateTime.UtcNow.Ticks:X}{Random.Shared.Next(1000, 9999)}";
    }
}
