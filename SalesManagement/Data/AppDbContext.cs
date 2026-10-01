using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Models;
using SalesManagement.Models.Enums;

namespace SalesManagement.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<WarehouseItem> WarehouseItems => Set<WarehouseItem>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<WarrantyHistory> WarrantyHistories => Set<WarrantyHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Banner> Banners => Set<Banner>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(u => u.FullName).HasMaxLength(200);
        builder.Entity<ApplicationUser>()
            .Property(u => u.Phone).HasMaxLength(20);
        builder.Entity<ApplicationUser>()
            .Property(u => u.Address).HasMaxLength(500);

        // Category (self-referencing parent/children)
        builder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Brand
        builder.Entity<Brand>(e =>
        {
            e.HasIndex(b => b.Name).IsUnique();
            e.Property(b => b.Name).HasMaxLength(200).IsRequired();
        });

        // Product
        builder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(300).IsRequired();
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductImage (cascade delete with product)
        builder.Entity<ProductImage>(e =>
        {
            e.Property(i => i.ImageUrl).HasMaxLength(500).IsRequired();
            e.HasOne(i => i.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductReview
        builder.Entity<ProductReview>(e =>
        {
            e.Property(r => r.Rating).IsRequired();
            e.HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WarehouseItem
        builder.Entity<WarehouseItem>(e =>
        {
            e.HasIndex(w => w.Barcode).IsUnique();
            e.HasIndex(w => w.SerialNumber).IsUnique();
            e.Property(w => w.Barcode).HasMaxLength(100).IsRequired();
            e.Property(w => w.SerialNumber).HasMaxLength(100).IsRequired();
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(w => w.Product)
                .WithMany(p => p.WarehouseItems)
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(w => w.ReservedOrder)
                .WithMany()
                .HasForeignKey(w => w.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SalesOrder -> table "orders"
        builder.Entity<SalesOrder>(e =>
        {
            e.ToTable("orders");
            e.Property(o => o.SubTotal).HasPrecision(18, 2);
            e.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            e.Property(o => o.TotalAmount).HasPrecision(18, 2);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(o => o.Promotion)
                .WithMany()
                .HasForeignKey(o => o.PromotionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // OrderItem (cascade delete with order)
        builder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.Price).HasPrecision(18, 2);
            e.Property(i => i.SubTotal).HasPrecision(18, 2);
            e.HasOne(i => i.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment (1-1 with order)
        builder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.TransactionCode).HasMaxLength(100);
            e.HasIndex(p => p.TransactionCode).IsUnique()
                .HasFilter("[TransactionCode] IS NOT NULL");
            e.HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Invoice (1-1 with order)
        builder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.HasOne(i => i.Order)
                .WithOne(o => o.Invoice)
                .HasForeignKey<Invoice>(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Promotion
        builder.Entity<Promotion>(e =>
        {
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Code).HasMaxLength(50).IsRequired();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.DiscountType).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.DiscountValue).HasPrecision(18, 2);
            e.Property(p => p.MinOrderValue).HasPrecision(18, 2);
        });

        // Warranty
        builder.Entity<Warranty>(e =>
        {
            e.HasIndex(w => w.SerialNumber).IsUnique();
            e.Property(w => w.SerialNumber).HasMaxLength(100).IsRequired();
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(w => w.WarehouseItem)
                .WithMany()
                .HasForeignKey(w => w.WarehouseItemId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(w => w.OrderItem)
                .WithMany()
                .HasForeignKey(w => w.OrderItemId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WarrantyHistory (cascade delete with warranty)
        builder.Entity<WarrantyHistory>(e =>
        {
            e.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(h => h.Warranty)
                .WithMany(w => w.History)
                .HasForeignKey(h => h.WarrantyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        builder.Entity<Notification>(e =>
        {
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Banner
        builder.Entity<Banner>(e =>
        {
            e.Property(b => b.Title).HasMaxLength(200).IsRequired();
            e.Property(b => b.ImageUrl).HasMaxLength(500);
            e.Property(b => b.LinkUrl).HasMaxLength(500);
        });
    }

    public override int SaveChanges()
    {
        SetCreatedAt();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetCreatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetCreatedAt()
    {
        var entries = ChangeTracker.Entries<IHasCreatedAt>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}
