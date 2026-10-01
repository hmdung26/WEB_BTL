using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class Warranty
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public WarrantyStatus Status { get; set; }
    public string? Note { get; set; }

    public int? WarehouseItemId { get; set; }
    public WarehouseItem? WarehouseItem { get; set; }

    public int? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public ICollection<WarrantyHistory> History { get; set; } = new List<WarrantyHistory>();
}
