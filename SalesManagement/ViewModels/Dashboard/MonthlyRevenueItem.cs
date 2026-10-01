namespace SalesManagement.ViewModels;

public class MonthlyRevenueItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }

    public string Label => $"{Month:00}/{Year}";
}
