using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class WarrantyHistory : IHasCreatedAt
{
    public int Id { get; set; }
    public WarrantyStatus Status { get; set; }
    public string? Note { get; set; }

    public int WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
