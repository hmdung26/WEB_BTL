using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class Payment
{
    public int Id { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionCode { get; set; }
    public DateTime? PaidAt { get; set; }

    public int OrderId { get; set; }
    public SalesOrder Order { get; set; } = null!;
}
