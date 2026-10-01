using SalesManagement.Models;

namespace SalesManagement.ViewModels;

public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalUsers { get; set; }
    public int TotalWarehouseItems { get; set; }
    public int AvailableWarehouseItems { get; set; }

    public List<MonthlyRevenueItem> MonthlyRevenue { get; set; } = new();
    public List<SalesOrder> RecentOrders { get; set; } = new();
}
