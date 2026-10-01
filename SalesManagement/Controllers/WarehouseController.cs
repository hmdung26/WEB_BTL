using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class WarehouseController : Controller
{
    private readonly AppDbContext _context;

    public WarehouseController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? productId, string? status, string? keyword)
    {
        var query = _context.WarehouseItems
            .Include(w => w.Product)
            .Include(w => w.ReservedOrder)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(w => w.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WarehouseItemStatus>(status, true, out var parsed))
        {
            query = query.Where(w => w.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(w =>
                w.SerialNumber.Contains(keyword) || w.Barcode.Contains(keyword) || w.ShelfLocation!.Contains(keyword));
        }

        var items = await query
            .OrderByDescending(w => w.Id)
            .Take(500)
            .ToListAsync();

        var model = new WarehouseListViewModel
        {
            Items = items,
            ProductId = productId,
            Status = status,
            Keyword = keyword,
            TotalCount = items.Count,
            ProductOptions = await _context.Products
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new WarehouseFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WarehouseFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var barcodeExists = await _context.WarehouseItems.AnyAsync(w => w.Barcode == model.Barcode);
        if (barcodeExists)
        {
            ModelState.AddModelError(nameof(model.Barcode), "Mã vạch đã tồn tại.");
        }

        var serialExists = await _context.WarehouseItems.AnyAsync(w => w.SerialNumber == model.SerialNumber);
        if (serialExists)
        {
            ModelState.AddModelError(nameof(model.SerialNumber), "Số serial đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.WarehouseItems.Add(new WarehouseItem
        {
            Barcode = model.Barcode,
            SerialNumber = model.SerialNumber,
            ShelfLocation = model.ShelfLocation,
            Status = model.Status,
            ProductId = model.ProductId,
            LastUpdated = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Thêm đơn vị hàng vào kho thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.WarehouseItems.FirstOrDefaultAsync(w => w.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        var model = new WarehouseFormViewModel
        {
            Id = item.Id,
            Barcode = item.Barcode,
            SerialNumber = item.SerialNumber,
            ShelfLocation = item.ShelfLocation,
            Status = item.Status,
            ProductId = item.ProductId
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WarehouseFormViewModel model)
    {
        var item = await _context.WarehouseItems.FirstOrDefaultAsync(w => w.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var barcodeExists = await _context.WarehouseItems.AnyAsync(w => w.Barcode == model.Barcode && w.Id != id);
        if (barcodeExists)
        {
            ModelState.AddModelError(nameof(model.Barcode), "Mã vạch đã tồn tại.");
        }

        var serialExists = await _context.WarehouseItems.AnyAsync(w => w.SerialNumber == model.SerialNumber && w.Id != id);
        if (serialExists)
        {
            ModelState.AddModelError(nameof(model.SerialNumber), "Số serial đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        item.Barcode = model.Barcode;
        item.SerialNumber = model.SerialNumber;
        item.ShelfLocation = model.ShelfLocation;
        item.Status = model.Status;
        item.ProductId = model.ProductId;
        item.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Cập nhật đơn vị hàng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, WarehouseItemStatus status, string? shelfLocation)
    {
        var item = await _context.WarehouseItems.FirstOrDefaultAsync(w => w.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = status;
        if (!string.IsNullOrWhiteSpace(shelfLocation))
        {
            item.ShelfLocation = shelfLocation;
        }

        // Releasing a reserved item detaches it from its order.
        if (status == WarehouseItemStatus.Available && item.OrderId.HasValue)
        {
            item.OrderId = null;
        }

        item.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Đã cập nhật trạng thái cho serial {item.SerialNumber}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.WarehouseItems.FirstOrDefaultAsync(w => w.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        _context.WarehouseItems.Remove(item);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Đã xóa đơn vị hàng khỏi kho.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(WarehouseFormViewModel model)
    {
        model.ProductOptions = await _context.Products
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();
    }
}
