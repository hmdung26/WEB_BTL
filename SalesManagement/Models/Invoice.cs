namespace SalesManagement.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public DateTime IssuedAt { get; set; }

    public int OrderId { get; set; }
    public SalesOrder Order { get; set; } = null!;
}
