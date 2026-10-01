using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models.Enums;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Services.Implementations;

public class CartService : ICartService
{
    private const string CartSessionKey = "Cart";
    private const string PromoSessionKey = "CartPromo";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;

    public CartService(IHttpContextAccessor httpContextAccessor, AppDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;

    public async Task<CartViewModel> GetCartAsync()
    {
        var cart = LoadCart();
        var model = new CartViewModel { Items = cart };

        var promoCode = Session.GetString(PromoSessionKey);
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            model.PromoCode = promoCode;
            var promo = await GetValidPromotionAsync(promoCode);
            if (promo is not null)
            {
                model.DiscountAmount = CalculateDiscount(promo, model.SubTotal);
                model.PromoApplied = true;
                model.PromoMessage = $"Đã áp dụng mã {promo.Code}.";
            }
            else
            {
                model.PromoMessage = "Mã giảm giá không còn hiệu lực.";
            }
        }

        return model;
    }

    public async Task<CartItem?> GetItemAsync(int productId)
    {
        var cart = LoadCart();
        return cart.FirstOrDefault(i => i.ProductId == productId);
    }

    public async Task AddItemAsync(int productId, int quantity)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null || quantity <= 0)
        {
            return;
        }

        var cart = LoadCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            item = new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                Quantity = 0
            };
            cart.Add(item);
        }

        item.Quantity = Math.Min(item.Quantity + quantity, product.StockQuantity);
        SaveCart(cart);
    }

    public async Task UpdateQuantityAsync(int productId, int quantity)
    {
        var cart = LoadCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            return;
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        var max = product?.StockQuantity ?? item.Quantity;

        if (quantity <= 0)
        {
            cart.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(quantity, max);
        }

        SaveCart(cart);
    }

    public Task RemoveItemAsync(int productId)
    {
        var cart = LoadCart();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null)
        {
            cart.Remove(item);
            SaveCart(cart);
        }

        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Session.Remove(CartSessionKey);
        Session.Remove(PromoSessionKey);
        return Task.CompletedTask;
    }

    public async Task ApplyPromotionAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Session.Remove(PromoSessionKey);
            return;
        }

        var promo = await GetValidPromotionAsync(code.Trim());
        if (promo is null)
        {
            Session.Remove(PromoSessionKey);
            return;
        }

        var cart = LoadCart();
        var subTotal = cart.Sum(i => i.SubTotal);
        if (subTotal < promo.MinOrderValue)
        {
            Session.Remove(PromoSessionKey);
            return;
        }

        Session.SetString(PromoSessionKey, promo.Code);
    }

    public Task ClearPromotionAsync()
    {
        Session.Remove(PromoSessionKey);
        return Task.CompletedTask;
    }

    private async Task<Models.Promotion?> GetValidPromotionAsync(string code)
    {
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

        return promo;
    }

    private decimal CalculateDiscount(Models.Promotion promo, decimal subTotal)
    {
        return promo.DiscountType == PromotionDiscountType.Percent
            ? Math.Round(subTotal * promo.DiscountValue / 100m, 2)
            : Math.Min(promo.DiscountValue, subTotal);
    }

    private List<CartItem> LoadCart()
    {
        var json = Session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<CartItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }
        catch
        {
            return new List<CartItem>();
        }
    }

    private void SaveCart(List<CartItem> cart)
    {
        Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
    }
}
