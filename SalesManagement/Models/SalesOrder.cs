using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class SalesOrder : IHasCreatedAt
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = null!;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public Payment? Payment { get; set; }
    public Invoice? Invoice { get; set; }
}
