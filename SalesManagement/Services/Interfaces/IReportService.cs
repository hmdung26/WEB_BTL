using SalesManagement.ViewModels;

namespace SalesManagement.Services.Interfaces;

public interface IReportService
{
    Task<DashboardViewModel> GetDashboardAsync();
    Task<List<MonthlyRevenueItem>> GetMonthlyRevenueAsync(int months = 12);
    Task<string> BuildReportPromptAsync();
}
