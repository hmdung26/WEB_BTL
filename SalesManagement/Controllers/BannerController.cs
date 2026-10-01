using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class BannerController : Controller
{
    private readonly AppDbContext _context;
    private readonly IFileUploadService _fileUploadService;

    public BannerController(AppDbContext context, IFileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var banners = await _context.Banners
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Id)
            .ToListAsync();

        return View(banners);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View(new BannerFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BannerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var banner = new Banner
        {
            Title = model.Title,
            Subtitle = model.Subtitle,
            LinkUrl = model.LinkUrl,
            Active = model.Active,
            SortOrder = model.SortOrder
        };

        if (model.ImageFile is not null)
        {
            banner.ImageUrl = await _fileUploadService.SaveImageAsync(model.ImageFile, "banners");
        }
        else
        {
            banner.ImageUrl = model.ImageUrl;
        }

        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Tạo banner thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == id);
        if (banner is null)
        {
            return NotFound();
        }

        var model = new BannerFormViewModel
        {
            Id = banner.Id,
            Title = banner.Title,
            Subtitle = banner.Subtitle,
            ImageUrl = banner.ImageUrl,
            LinkUrl = banner.LinkUrl,
            Active = banner.Active,
            SortOrder = banner.SortOrder
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BannerFormViewModel model)
    {
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == id);
        if (banner is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        banner.Title = model.Title;
        banner.Subtitle = model.Subtitle;
        banner.LinkUrl = model.LinkUrl;
        banner.Active = model.Active;
        banner.SortOrder = model.SortOrder;

        if (model.ImageFile is not null)
        {
            var oldUrl = banner.ImageUrl;
            banner.ImageUrl = await _fileUploadService.SaveImageAsync(model.ImageFile, "banners");
            if (banner.ImageUrl != oldUrl)
            {
                _fileUploadService.DeleteFile(oldUrl);
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            banner.ImageUrl = model.ImageUrl;
        }

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Cập nhật banner thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == id);
        if (banner is null)
        {
            return NotFound();
        }

        _fileUploadService.DeleteFile(banner.ImageUrl);
        _context.Banners.Remove(banner);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Đã xóa banner.";
        return RedirectToAction(nameof(Index));
    }
}
