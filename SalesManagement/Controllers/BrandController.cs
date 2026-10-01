using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class BrandController : Controller
{
    private readonly AppDbContext _context;
    private readonly IFileUploadService _fileUploadService;

    public BrandController(AppDbContext context, IFileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var brands = await _context.Brands
            .OrderBy(b => b.Name)
            .ToListAsync();

        return View(brands);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View(new BrandFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BrandFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var brand = new Brand
        {
            Name = model.Name,
            Description = model.Description
        };

        if (model.LogoFile is not null)
        {
            brand.LogoUrl = await _fileUploadService.SaveImageAsync(model.LogoFile, "brands");
        }
        else
        {
            brand.LogoUrl = model.LogoUrl;
        }

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Tạo thương hiệu thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
        if (brand is null)
        {
            return NotFound();
        }

        var model = new BrandFormViewModel
        {
            Id = brand.Id,
            Name = brand.Name,
            Description = brand.Description,
            LogoUrl = brand.LogoUrl
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BrandFormViewModel model)
    {
        var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
        if (brand is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        brand.Name = model.Name;
        brand.Description = model.Description;

        if (model.LogoFile is not null)
        {
            var oldUrl = brand.LogoUrl;
            brand.LogoUrl = await _fileUploadService.SaveImageAsync(model.LogoFile, "brands");
            if (brand.LogoUrl != oldUrl)
            {
                _fileUploadService.DeleteFile(oldUrl);
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.LogoUrl))
        {
            brand.LogoUrl = model.LogoUrl;
        }

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Cập nhật thương hiệu thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
        if (brand is null)
        {
            return NotFound();
        }

        var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
        if (hasProducts)
        {
            TempData["Error"] = "Không thể xóa thương hiệu đang có sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        _fileUploadService.DeleteFile(brand.LogoUrl);
        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Đã xóa thương hiệu.";
        return RedirectToAction(nameof(Index));
    }
}
