using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IFileUploadService _fileUploadService;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductController(
        IProductService productService,
        IFileUploadService fileUploadService,
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _productService = productService;
        _fileUploadService = fileUploadService;
        _context = context;
        _userManager = userManager;
    }

    // ---- Customer-facing catalog ----

    [HttpGet]
    public async Task<IActionResult> Index(ProductFilter filter)
    {
        var model = await _productService.GetProductsAsync(filter);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            userId = user?.Id;
        }

        var model = await _productService.GetDetailsAsync(id, userId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Đánh giá không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var review = new ProductReview
        {
            ProductId = model.ProductId,
            UserId = user.Id,
            Rating = model.Rating,
            Comment = model.Comment
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm.";
        return RedirectToAction(nameof(Details), new { id = model.ProductId });
    }

    // ---- Admin CRUD ----

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Manage()
    {
        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return View(products);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var model = new ProductFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            Specifications = model.Specifications,
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            WarrantyPeriod = model.WarrantyPeriod,
            BrandId = model.BrandId,
            CategoryId = model.CategoryId
        };

        // Main image (upload or URL)
        if (model.ImageFile is not null)
        {
            product.ImageUrl = await _fileUploadService.SaveImageAsync(model.ImageFile, "products");
        }
        else
        {
            product.ImageUrl = model.ImageUrl;
        }

        // Additional images
        if (model.ImageFiles is not null)
        {
            var order = 0;
            foreach (var file in model.ImageFiles)
            {
                var url = await _fileUploadService.SaveImageAsync(file, "products");
                if (url is not null)
                {
                    product.ProductImages.Add(new ProductImage { ImageUrl = url, SortOrder = order++ });
                }
            }
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Tạo sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Specifications = product.Specifications,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            WarrantyPeriod = product.WarrantyPeriod,
            ImageUrl = product.ImageUrl,
            BrandId = product.BrandId,
            CategoryId = product.CategoryId,
            ExistingImages = product.ProductImages
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, SortOrder = i.SortOrder })
                .ToList()
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model);
        model.ExistingImages = product.ProductImages
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, SortOrder = i.SortOrder })
            .ToList();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        product.Name = model.Name;
        product.Description = model.Description;
        product.Specifications = model.Specifications;
        product.Price = model.Price;
        product.StockQuantity = model.StockQuantity;
        product.WarrantyPeriod = model.WarrantyPeriod;
        product.BrandId = model.BrandId;
        product.CategoryId = model.CategoryId;

        if (model.ImageFile is not null)
        {
            var oldUrl = product.ImageUrl;
            product.ImageUrl = await _fileUploadService.SaveImageAsync(model.ImageFile, "products");
            if (product.ImageUrl != oldUrl)
            {
                _fileUploadService.DeleteFile(oldUrl);
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            product.ImageUrl = model.ImageUrl;
        }

        if (model.ImageFiles is not null)
        {
            var order = product.ProductImages.Count;
            foreach (var file in model.ImageFiles)
            {
                var url = await _fileUploadService.SaveImageAsync(file, "products");
                if (url is not null)
                {
                    product.ProductImages.Add(new ProductImage { ImageUrl = url, SortOrder = order++ });
                }
            }
        }

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Cập nhật sản phẩm thành công.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);

        if (image is not null)
        {
            _fileUploadService.DeleteFile(image.ImageUrl);
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        _fileUploadService.DeleteFile(product.ImageUrl);
        foreach (var image in product.ProductImages)
        {
            _fileUploadService.DeleteFile(image.ImageUrl);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Đã xóa sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(ProductFormViewModel model)
    {
        model.BrandOptions = await _context.Brands
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        model.CategoryOptions = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }
}
