using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models.Enums;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Services.Implementations;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var revenue = await _context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var totalOrders = await _context.Orders.CountAsync();
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var totalProducts = await _context.Products.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        var totalWarehouse = await _context.WarehouseItems.CountAsync();
        var availableWarehouse = await _context.WarehouseItems.CountAsync(w => w.Status == WarehouseItemStatus.Available);

        var recentOrders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.Id)
            .Take(8)
            .ToListAsync();

        return new DashboardViewModel
        {
            TotalRevenue = revenue,
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            TotalProducts = totalProducts,
            TotalUsers = totalUsers,
            TotalWarehouseItems = totalWarehouse,
            AvailableWarehouseItems = availableWarehouse,
            MonthlyRevenue = await GetMonthlyRevenueAsync(12),
            RecentOrders = recentOrders
        };
    }

    public async Task<List<MonthlyRevenueItem>> GetMonthlyRevenueAsync(int months = 12)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(months - 1));

        var rows = await _context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled && o.CreatedAt >= start)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .ToListAsync();

        var map = rows.ToDictionary(r => (r.Year, r.Month));

        var result = new List<MonthlyRevenueItem>();
        var cursor = start;

        for (var i = 0; i < months; i++)
        {
            var key = (cursor.Year, cursor.Month);
            result.Add(new MonthlyRevenueItem
            {
                Year = key.Year,
                Month = key.Month,
                Revenue = map.TryGetValue(key, out var r) ? r.Revenue : 0m,
                OrderCount = map.TryGetValue(key, out r) ? r.OrderCount : 0
            });
            cursor = cursor.AddMonths(1);
        }

        return result;
    }

    public async Task<string> BuildReportPromptAsync()
    {
        var d = await GetDashboardAsync();
        var monthly = string.Join("; ",
            d.MonthlyRevenue.Select(m => $"tháng {m.Month:00}/{m.Year}: {m.Revenue:N0} đ ({m.OrderCount} đơn)"));

        return $@"Đây là số liệu kinh doanh hiện tại của cửa hàng:
- Tổng doanh thu: {d.TotalRevenue:N0} đ
- Tổng đơn hàng: {d.TotalOrders} (đang chờ xử lý: {d.PendingOrders})
- Tổng sản phẩm: {d.TotalProducts}
- Tổng người dùng: {d.TotalUsers}
- Tồn kho: {d.AvailableWarehouseItems}/{d.TotalWarehouseItems} đơn vị sẵn sàng
- Doanh thu 12 tháng gần nhất: {monthly}

Hãy viết một báo cáo ngắn gọn bằng tiếng Việt đánh giá tình hình kinh doanh và đưa ra vài gợi ý cải thiện.";
    }
}
