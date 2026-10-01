using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

[Authorize]
public class WarrantyController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WarrantyController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ---- Customer: look up warranty by serial ----

    [HttpGet]
    public IActionResult Lookup()
    {
        return View(new WarrantyLookupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lookup(WarrantyLookupViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SerialNumber))
        {
            return View(model);
        }

        model.Warranty = await _context.Warranties
            .Include(w => w.WarehouseItem).ThenInclude(wi => wi!.Product)
            .Include(w => w.OrderItem).ThenInclude(oi => oi!.Product)
            .FirstOrDefaultAsync(w => w.SerialNumber == model.SerialNumber.Trim());

        if (model.Warranty is not null)
        {
            model.History = await _context.WarrantyHistories
                .Where(h => h.WarrantyId == model.Warranty.Id)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
        else
        {
            model.WarehouseItem = await _context.WarehouseItems
                .Include(wi => wi.Product)
                .FirstOrDefaultAsync(wi => wi.SerialNumber == model.SerialNumber.Trim());
        }

        return View(model);
    }

    // ---- Customer: submit a warranty request ----

    [HttpGet]
    public IActionResult Submit()
    {
        return View(new WarrantyRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(WarrantyRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var serial = model.SerialNumber!.Trim();

        // The serial must belong to a sold warehouse item.
        var warehouseItem = await _context.WarehouseItems
            .Include(wi => wi.Product)
            .FirstOrDefaultAsync(wi => wi.SerialNumber == serial);

        if (warehouseItem is null || warehouseItem.Status != WarehouseItemStatus.Sold)
        {
            ModelState.AddModelError(nameof(model.SerialNumber), "Không tìm thấy sản phẩm đã bán với số serial này.");
            return View(model);
        }

        // Ensure no active warranty already exists for this serial.
        var existing = await _context.Warranties.AnyAsync(w => w.SerialNumber == serial);
        if (existing)
        {
            ModelState.AddModelError(nameof(model.SerialNumber), "Sản phẩm này đã có hồ sơ bảo hành.");
            return View(model);
        }

        var warranty = new Warranty
        {
            SerialNumber = serial,
            WarehouseItemId = warehouseItem.Id,
            OrderItemId = warehouseItem.OrderId.HasValue
                ? await _context.OrderItems
                    .Where(oi => oi.OrderId == warehouseItem.OrderId && oi.ProductId == warehouseItem.ProductId)
                    .Select(oi => (int?)oi.Id)
                    .FirstOrDefaultAsync()
                : null,
            UserId = user.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(warehouseItem.Product.WarrantyPeriod),
            Status = WarrantyStatus.Requested,
            Note = model.Note
        };

        warranty.History.Add(new WarrantyHistory
        {
            Status = WarrantyStatus.Requested,
            Note = "Khách hàng gửi yêu cầu bảo hành." + (string.IsNullOrWhiteSpace(model.Note) ? "" : $" Mô tả: {model.Note}")
        });

        _context.Warranties.Add(warranty);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Yêu cầu bảo hành đã được gửi. Vui lòng chờ nhân viên xử lý.";
        return RedirectToAction(nameof(Lookup));
    }

    // ---- Staff/Admin: manage warranty requests ----

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Index()
    {
        var warranties = await _context.Warranties
            .Include(w => w.User)
            .Include(w => w.WarehouseItem).ThenInclude(wi => wi!.Product)
            .OrderByDescending(w => w.Id)
            .ToListAsync();

        return View(warranties);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, WarrantyStatus status, string? note)
    {
        var warranty = await _context.Warranties.FirstOrDefaultAsync(w => w.Id == id);
        if (warranty is null)
        {
            return NotFound();
        }

        warranty.Status = status;
        warranty.Note = note;
        warranty.History.Add(new WarrantyHistory
        {
            Status = status,
            Note = note,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Đã cập nhật trạng thái bảo hành của serial {warranty.SerialNumber}.";
        return RedirectToAction(nameof(Index));
    }
}
