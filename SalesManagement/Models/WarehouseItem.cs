using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class WarehouseItem
{
    public int Id { get; set; }
    public string Barcode { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string? ShelfLocation { get; set; }
    public WarehouseItemStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? OrderId { get; set; }
    public SalesOrder? ReservedOrder { get; set; }
}
