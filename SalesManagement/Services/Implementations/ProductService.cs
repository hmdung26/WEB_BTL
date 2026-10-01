using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Services.Implementations;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductListViewModel> GetProductsAsync(ProductFilter filter)
    {
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 50);
        filter.Page = Math.Max(1, filter.Page);

        var query = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.WarehouseItems)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(kw) || (p.Description != null && p.Description.Contains(kw)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value
                || p.Category.ParentId == filter.CategoryId.Value);
        }

        if (filter.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == filter.BrandId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        query = filter.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.Id),
        };

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                BrandName = p.Brand.Name,
                CategoryName = p.Category.Name,
                StockQuantity = p.StockQuantity,
                ReviewCount = _context.ProductReviews.Count(r => r.ProductId == p.Id),
                AverageRating = _context.ProductReviews
                    .Where(r => r.ProductId == p.Id)
                    .Select(r => (double?)r.Rating)
                    .Average() ?? 0
            })
            .ToListAsync();

        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();

        var brands = await _context.Brands
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        return new ProductListViewModel
        {
            Items = products,
            Filter = filter,
            CategoryOptions = categories,
            BrandOptions = brands,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<ProductDetailsViewModel?> GetDetailsAsync(int id, int? currentUserId)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return null;
        }

        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProductReviewItemViewModel
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                UserName = r.User.FullName ?? r.User.UserName ?? "Khách hàng",
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        var averageRating = reviews.Count == 0 ? 0 : reviews.Average(r => r.Rating);

        var canReview = currentUserId.HasValue && await _context.OrderItems
            .AnyAsync(oi => oi.ProductId == id
                && oi.Order.UserId == currentUserId.Value
                && oi.Order.Status == Models.Enums.OrderStatus.Delivered);

        var imageUrls = product.ProductImages
            .OrderBy(i => i.SortOrder)
            .Select(i => i.ImageUrl)
            .ToList();

        if (imageUrls.Count == 0 && !string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            imageUrls.Insert(0, product.ImageUrl);
        }

        return new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Specifications = product.Specifications,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            WarrantyPeriod = product.WarrantyPeriod,
            BrandName = product.Brand.Name,
            CategoryName = product.Category.Name,
            ImageUrls = imageUrls,
            Reviews = reviews,
            AverageRating = averageRating,
            ReviewCount = reviews.Count,
            CanReview = canReview
        };
    }

    public async Task<List<ProductListItemViewModel>> GetNewestAsync(int count)
    {
        return await GetTopProductsAsync(count, _context.Products.OrderByDescending(p => p.Id));
    }

    public async Task<List<ProductListItemViewModel>> GetTopRatedAsync(int count)
    {
        return await GetTopProductsAsync(count,
            _context.Products.OrderByDescending(p =>
                _context.ProductReviews.Where(r => r.ProductId == p.Id).Select(r => (double?)r.Rating).Average() ?? 0));
    }

    public async Task<List<CategorySectionViewModel>> GetProductsByCategoryAsync(int perCategory)
    {
        // Nhóm sản phẩm theo danh mục gốc (cha), lấy đại diện vài sản phẩm mỗi danh mục.
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                CategoryId = c.ParentId ?? c.Id,
                Name = c.Parent != null ? c.Parent.Name : c.Name
            })
            .Distinct()
            .ToListAsync();

        var sections = new List<CategorySectionViewModel>();
        var usedCategoryIds = new HashSet<int>();

        foreach (var cat in categories)
        {
            if (!usedCategoryIds.Add(cat.CategoryId))
            {
                continue;
            }

            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.CategoryId == cat.CategoryId || p.Category.ParentId == cat.CategoryId)
                .OrderByDescending(p => p.Id)
                .Take(perCategory)
                .Select(p => new ProductListItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    BrandName = p.Brand.Name,
                    CategoryName = p.Category.Name,
                    StockQuantity = p.StockQuantity,
                    ReviewCount = _context.ProductReviews.Count(r => r.ProductId == p.Id),
                    AverageRating = _context.ProductReviews
                        .Where(r => r.ProductId == p.Id)
                        .Select(r => (double?)r.Rating)
                        .Average() ?? 0
                })
                .ToListAsync();

            if (products.Count > 0)
            {
                sections.Add(new CategorySectionViewModel
                {
                    CategoryId = cat.CategoryId,
                    Name = cat.Name,
                    Products = products
                });
            }
        }

        return sections;
    }

    private async Task<List<ProductListItemViewModel>> GetTopProductsAsync(int count, IOrderedQueryable<Product> ordered)
    {
        return await ordered
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsNoTracking()
            .Take(count)
            .Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                BrandName = p.Brand.Name,
                CategoryName = p.Category.Name,
                StockQuantity = p.StockQuantity,
                ReviewCount = _context.ProductReviews.Count(r => r.ProductId == p.Id),
                AverageRating = _context.ProductReviews
                    .Where(r => r.ProductId == p.Id)
                    .Select(r => (double?)r.Rating)
                    .Average() ?? 0
            })
            .ToListAsync();
    }
}
