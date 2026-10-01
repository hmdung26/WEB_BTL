using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Models;

namespace SalesManagement.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager);
        await SeedCatalogAsync(context);
        await SeedWarehouseAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        foreach (var role in new[] { "User", "Staff", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByNameAsync("admin") is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@example.com",
            FullName = "Administrator",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, "admin123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    private static async Task SeedCatalogAsync(AppDbContext context)
    {
        if (!await context.Banners.AnyAsync())
        {
            context.Banners.AddRange(
                new Banner { Title = "Khuyến mãi lớn", Subtitle = "Giảm giá đến 30%", SortOrder = 1, Active = true },
                new Banner { Title = "Sản phẩm mới về", Subtitle = "Công nghệ mới nhất", SortOrder = 2, Active = true });
        }

        if (await context.Categories.AnyAsync())
        {
            await context.SaveChangesAsync();
            return;
        }

        var electronics = new Category { Name = "Electronics", Description = "Điện tử" };
        var phones = new Category { Name = "Phones", Description = "Điện thoại", Parent = electronics };
        var laptops = new Category { Name = "Laptops", Description = "Máy tính xách tay", Parent = electronics };
        var fashion = new Category { Name = "Fashion", Description = "Thời trang" };

        var apple = new Brand { Name = "Apple", Description = "Apple Inc." };
        var samsung = new Brand { Name = "Samsung", Description = "Samsung Electronics" };

        context.Categories.AddRange(electronics, phones, laptops, fashion);
        context.Brands.AddRange(apple, samsung);

        context.Products.AddRange(
            new Product
            {
                Name = "iPhone 15 Pro Max",
                Description = "Điện thoại cao cấp của Apple",
                Specifications = "256GB, Titanium",
                Price = 34990000,
                StockQuantity = 50,
                WarrantyPeriod = 12,
                Category = phones,
                Brand = apple
            },
            new Product
            {
                Name = "Galaxy S24 Ultra",
                Description = "Điện thoại cao cấp của Samsung",
                Specifications = "512GB, Titanium",
                Price = 30990000,
                StockQuantity = 40,
                WarrantyPeriod = 12,
                Category = phones,
                Brand = samsung
            },
            new Product
            {
                Name = "MacBook Pro 14",
                Description = "Laptop chuyên nghiệp",
                Specifications = "M3 Pro, 18GB RAM",
                Price = 49990000,
                StockQuantity = 25,
                WarrantyPeriod = 24,
                Category = laptops,
                Brand = apple
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedWarehouseAsync(AppDbContext context)
    {
        // Ensure the per-serial layer matches the aggregate stock seeded above,
        // so the two inventory layers stay in sync from the start.
        if (await context.WarehouseItems.AnyAsync())
        {
            return;
        }

        var products = await context.Products.ToListAsync();
        var counter = 0;

        foreach (var product in products)
        {
            for (var i = 0; i < product.StockQuantity; i++)
            {
                counter++;
                context.WarehouseItems.Add(new WarehouseItem
                {
                    ProductId = product.Id,
                    Barcode = $"BC-{product.Id:D3}-{counter:D5}",
                    SerialNumber = $"SN-{product.Id:D3}-{counter:D5}",
                    Status = Models.Enums.WarehouseItemStatus.Available,
                    ShelfLocation = $"A-{(counter % 10) + 1:D2}",
                    LastUpdated = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
