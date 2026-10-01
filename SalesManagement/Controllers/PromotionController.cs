using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class PromotionController : Controller
{
    private readonly AppDbContext _context;

    public PromotionController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var promotions = await _context.Promotions
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return View(promotions);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View(new PromotionFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PromotionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.EndAt < model.StartAt)
        {
            ModelState.AddModelError(nameof(model.EndAt), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var exists = await _context.Promotions.AnyAsync(p => p.Code == model.Code);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Code), "Mã giảm giá đã tồn tại.");
            return View(model);
        }

        var promo = new Promotion
        {
            Code = model.Code,
            Name = model.Name,
            DiscountType = model.DiscountType,
            DiscountValue = model.DiscountValue,
            MinOrderValue = model.MinOrderValue,
            StartAt = model.StartAt,
            EndAt = model.EndAt,
            UsageLimit = model.UsageLimit,
            Active = model.Active
        };

        _context.Promotions.Add(promo);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Tạo mã giảm giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        if (promo is null)
        {
            return NotFound();
        }

        var model = new PromotionFormViewModel
        {
            Id = promo.Id,
            Code = promo.Code,
            Name = promo.Name,
            DiscountType = promo.DiscountType,
            DiscountValue = promo.DiscountValue,
            MinOrderValue = promo.MinOrderValue,
            StartAt = promo.StartAt,
            EndAt = promo.EndAt,
            UsageLimit = promo.UsageLimit,
            Active = promo.Active
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PromotionFormViewModel model)
    {
        var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        if (promo is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.EndAt < model.StartAt)
        {
            ModelState.AddModelError(nameof(model.EndAt), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        promo.Code = model.Code;
        promo.Name = model.Name;
        promo.DiscountType = model.DiscountType;
        promo.DiscountValue = model.DiscountValue;
        promo.MinOrderValue = model.MinOrderValue;
        promo.StartAt = model.StartAt;
        promo.EndAt = model.EndAt;
        promo.UsageLimit = model.UsageLimit;
        promo.Active = model.Active;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Cập nhật mã giảm giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        if (promo is null)
        {
            return NotFound();
        }

        _context.Promotions.Remove(promo);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Đã xóa mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }
}
