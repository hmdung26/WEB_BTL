using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public OrderController(
        ICartService cartService,
        IOrderService orderService,
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    // ---- Customer checkout ----

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var user = await _userManager.GetUserAsync(User);
        var model = new CheckoutViewModel
        {
            CustomerName = user?.FullName ?? string.Empty,
            CustomerPhone = user?.Phone,
            CustomerAddress = user?.Address,
            Cart = cart
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model.Cart = await _cartService.GetCartAsync();
            return View(model);
        }

        try
        {
            var order = await _orderService.CreateOrderFromCartAsync(
                user, model.CustomerName, model.CustomerPhone, model.CustomerAddress);

            TempData["StatusMessage"] = $"Đặt hàng thành công. Mã đơn: #{order.Id}";
            return RedirectToAction(nameof(My));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Cart = await _cartService.GetCartAsync();
            return View(model);
        }
    }

    // ---- Customer's own orders ----

    [HttpGet]
    public async Task<IActionResult> My()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var orders = await _context.Orders
            .Where(o => o.UserId == user.Id)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Staff");

        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Include(o => o.Invoice)
            .Include(o => o.Promotion)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (!isStaff && order.UserId != user?.Id)
        {
            return Forbid();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        try
        {
            await _orderService.CancelOrderAsync(id, user.Id);
            TempData["StatusMessage"] = "Đã hủy đơn hàng.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(My));
    }

    // ---- Staff/Admin: all orders + status management ----

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.User)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        // When an order is delivered, mark its reserved warehouse items as sold.
        if (status == OrderStatus.Delivered && order.Status != OrderStatus.Delivered)
        {
            var items = await _context.WarehouseItems
                .Where(w => w.OrderId == order.Id && w.Status == WarehouseItemStatus.Reserved)
                .ToListAsync();

            foreach (var wi in items)
            {
                wi.Status = WarehouseItemStatus.Sold;
                wi.LastUpdated = DateTime.UtcNow;
            }
        }

        // When cancelled, restore stock + release reserved items (reuse order service).
        if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product is not null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }

            var reserved = await _context.WarehouseItems
                .Where(w => w.OrderId == order.Id && w.Status == WarehouseItemStatus.Reserved)
                .ToListAsync();

            foreach (var wi in reserved)
            {
                wi.Status = WarehouseItemStatus.Available;
                wi.OrderId = null;
                wi.LastUpdated = DateTime.UtcNow;
            }
        }

        order.Status = status;
        await _context.SaveChangesAsync();

        if (order.UserId.HasValue)
        {
            await _notificationService.CreateAsync(order.UserId.Value, $"Đơn hàng #{order.Id} cập nhật trạng thái",
                $"Đơn hàng của bạn đã chuyển sang trạng thái: {status}.");
        }

        TempData["StatusMessage"] = "Đã cập nhật trạng thái đơn hàng.";
        return RedirectToAction(nameof(Index));
    }
}
