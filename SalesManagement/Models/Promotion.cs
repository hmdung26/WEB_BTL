using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class Promotion : IHasCreatedAt
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public PromotionDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinOrderValue { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }
}
